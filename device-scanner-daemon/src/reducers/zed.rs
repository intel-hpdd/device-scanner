use device_types::{
    state,
    zed::{prop, zfs, zpool, PoolCommand},
};
use error::{Error, Result};
use libzfs_types;
use std::result;

pub fn into_zed_events(xs: Vec<libzfs_types::Pool>) -> state::ZedEvents {
    xs.into_iter().map(|p| (p.guid, p)).collect()
}

pub fn take_pool(zed_events: &mut state::ZedEvents, guid: u64) -> Result<libzfs_types::Pool> {
    zed_events.remove(&guid).ok_or_else(|| {
        Error::LibZfsError(libzfs_types::LibZfsError::PoolNotFound(None, Some(guid)))
    })
}

fn update_prop(name: &str, value: &str, xs: Vec<libzfs_types::ZProp>) -> Vec<libzfs_types::ZProp> {
    let mut xs: Vec<libzfs_types::ZProp> = xs.into_iter().filter(|z| z.name != name).collect();

    xs.push(libzfs_types::ZProp {
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
    cmd: PoolCommand,
) -> Result<state::ZedEvents> {
    match cmd {
        PoolCommand::AddPools(pools) => Ok(into_zed_events(pools)),
        PoolCommand::AddPool(pool) | PoolCommand::UpdatePool(pool) => {
            Ok(zed_events.update(pool.guid, pool))
        }
        PoolCommand::RemovePool(guid) => {
            let guid = guid_to_u64(guid)?;

            take_pool(&mut zed_events, guid)?;

            Ok(zed_events)
        }
        PoolCommand::AddDataset(guid, dataset) => {
            let guid = guid_to_u64(guid)?;

            let mut pool = take_pool(&mut zed_events, guid)?;

            let mut ds: Vec<libzfs_types::Dataset> = pool
                .datasets
                .into_iter()
                .filter(|d| d.name != dataset.name)
                .collect();

            ds.push(dataset);

            pool.datasets = ds;

            Ok(zed_events)
        }
        PoolCommand::RemoveDataset(guid, zfs::Name(name)) => {
            let guid = guid_to_u64(guid)?;

            let mut pool = take_pool(&mut zed_events, guid)?;

            pool.datasets = pool
                .datasets
                .into_iter()
                .filter(|d| d.name != name)
                .collect();

            Ok(zed_events)
        }
        PoolCommand::SetZpoolProp(guid, prop::Key(key), prop::Value(value)) => {
            let guid = guid_to_u64(guid)?;

            let mut pool = take_pool(&mut zed_events, guid)?;

            pool.props = update_prop(&key, &value, pool.props);

            Ok(zed_events)
        }
        PoolCommand::SetZfsProp(guid, zfs::Name(name), prop::Key(key), prop::Value(value)) => {
            let guid = guid_to_u64(guid)?;

            fn get_dataset_in_pool(
                pool: libzfs_types::Pool,
                name: String,
            ) -> Result<libzfs_types::Dataset> {
                pool.datasets
                    .into_iter()
                    .find(|d| d.name == name)
                    .ok_or_else(|| Error::LibZfsError(libzfs_types::LibZfsError::ZfsNotFound(name)))
            }

            let mut pool = take_pool(&mut zed_events, guid)?;
            let mut dataset = get_dataset_in_pool(pool, name)?;

            dataset.props = update_prop(&key, &value, dataset.props);

            Ok(zed_events)
        }
    }
}
