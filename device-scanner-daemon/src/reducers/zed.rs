use device_types::{
    state,
    zed::{prop, zfs, zpool, ZedCommand},
};
use error::{Error, Result};
use std::result;

pub fn into_zed_events(xs: Vec<libzfs_types::Pool>) -> state::ZedEvents {
    xs.into_iter().map(|p| (p.guid, p)).collect()
}

pub fn take_pool(zed_events: &mut state::ZedEvents, guid: u64) -> Result<libzfs_types::Pool> {
    zed_events
        .remove(&guid)
        .ok_or_else(|| Error::LibZfsError(libzfs::LibZfsError::PoolNotFound(None, Some(guid))))
}

fn update_prop(name: &str, value: &str, xs: Vec<libzfs::ZProp>) -> Vec<libzfs::ZProp> {
    let mut xs: Vec<libzfs::ZProp> = xs.into_iter().filter(|z| z.name != name).collect();

    xs.push(libzfs::ZProp {
        name: name.to_string(),
        value: value.to_string(),
    });

    xs
}

fn guid_to_u64(guid: zpool::Guid) -> Result<u64> {
    let guid: result::Result<u64, std::num::ParseIntError> = guid.into();
    Ok(guid?)
}

/// Mutably updates the Zed portion of the device map in response to `ZedCommand`s.
pub fn update_zed_events(
    mut zed_events: state::ZedEvents,
    cmd: ZedCommand,
) -> Result<state::ZedEvents> {
    let zed_events = match cmd {
        ZedCommand::Init => {
            let pools = libzfs::get_imported_pools()?;
            let pools = into_zed_events(pools);

            Ok(pools)
        }
        ZedCommand::CreateZpool(zpool::Name(name), guid, _) => {
            let guid = guid_to_u64(guid)?;
            let pool = libzfs::get_pool_by_name_and_guid(&name, guid)?;

            Ok(zed_events.update(pool.guid, pool))
        }
        ZedCommand::ImportZpool(zpool::Name(name), guid, zpool::State(state)) => {
            let guid = guid_to_u64(guid)?;

            take_pool(&mut zed_events, guid)
                .map(|mut x| {
                    x.state = state;
                    x.name = name.clone();
                    x
                }).or_else(|_| {
                    libzfs::get_pool_by_name_and_guid(&name, guid).map_err(Error::LibZfsError)
                }).map(|x| zed_events.update(guid, x))
        }
        ZedCommand::ExportZpool(guid, _) | ZedCommand::DestroyZpool(guid) => {
            let guid = guid_to_u64(guid)?;

            take_pool(&mut zed_events, guid)?;

            Ok(zed_events)
        }
        ZedCommand::CreateZfs(guid, zfs::Name(name)) => {
            let guid = guid_to_u64(guid)?;

            let mut pool = take_pool(&mut zed_events, guid)?;

            let dataset = libzfs::get_dataset_by_name(&name)?;

            let mut ds: Vec<libzfs_types::Dataset> = pool
                .datasets
                .into_iter()
                .filter(|d| d.name != name)
                .collect();

            ds.push(dataset);

            pool.datasets = ds;

            Ok(zed_events)
        }
        ZedCommand::DestroyZfs(guid, zfs::Name(name)) => {
            let guid = guid_to_u64(guid)?;

            let mut pool = take_pool(&mut zed_events, guid)?;

            pool.datasets = pool
                .datasets
                .into_iter()
                .filter(|d| d.name != name)
                .collect();

            Ok(zed_events)
        }
        ZedCommand::SetZpoolProp(guid, prop::Key(key), prop::Value(value)) => {
            let guid = guid_to_u64(guid)?;

            let mut pool = take_pool(&mut zed_events, guid)?;

            pool.props = update_prop(&key, &value, pool.props);

            Ok(zed_events)
        }
        ZedCommand::SetZfsProp(guid, zfs::Name(name), prop::Key(key), prop::Value(value)) => {
            let guid = guid_to_u64(guid)?;

            fn get_dataset_in_pool(
                pool: libzfs_types::Pool,
                name: String,
            ) -> Result<libzfs_types::Dataset> {
                pool.datasets
                    .into_iter()
                    .find(|d| d.name == name)
                    .ok_or_else(|| Error::LibZfsError(libzfs::LibZfsError::ZfsNotFound(name)))
            }

            let mut pool = take_pool(&mut zed_events, guid)?;
            let mut dataset = get_dataset_in_pool(pool, name)?;

            dataset.props = update_prop(&key, &value, dataset.props);

            Ok(zed_events)
        }
        ZedCommand::AddVdev(guid) => {
            let guid = guid_to_u64(guid)?;

            let pool = take_pool(&mut zed_events, guid)?;
            let pool = libzfs::get_pool_by_name_and_guid(&pool.name, guid)?;

            Ok(zed_events.update(guid, pool))
        }
    };

    Ok(zed_events?)
}
