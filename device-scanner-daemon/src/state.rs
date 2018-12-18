// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

//! Handles Updates of state
//!
//! `device-scanner` uses a persistent streaming strategy
//! where Unix domain sockets can connect and be fed device-graph changes as they occur.
//! This module is responsible for internally storing the current state and building the next device-graph
//! after each "tick" (an incoming device event).

use futures::future::Future;
use futures::sync::mpsc::{self, UnboundedSender};

use im::{HashSet, OrdSet};
use serde_json;
use std::{io, iter::IntoIterator, path::PathBuf};
use tokio::prelude::*;

use device_types::{
    devices::{self, Device, Parents, Serial},
    mount::{BdevPath, FsType, Mount, MountPoint},
    state,
    uevent::UEvent,
    Command,
};

use crate::{
    connections,
    error::{self, Result},
    reducers::{mount::update_mount, udev::update_udev, zed::update_zed_events},
};

pub fn format_major_minor(major: &str, minor: &str) -> String {
    format!("{}:{}", major, minor)
}

pub fn find_by_major_minor(xs: &im::Vector<String>, major: &str, minor: &str) -> bool {
    xs.contains(&format_major_minor(major, minor))
}

/// Do the provided OrdSets share any paths.
fn do_paths_intersect(p1: &OrdSet<PathBuf>, p2: &OrdSet<PathBuf>) -> bool {
    p1.iter().any(|p| p2.contains(p))
}

fn get_parents(xs: &state::UEvents, f: impl Fn(&UEvent) -> bool) -> Result<Parents> {
    xs.values()
        .filter(|y| y.keep_usable())
        .filter(|&x| f(x))
        .map(|x| {
            let serial = x.get_serial()?;

            Ok((x.get_type(), serial))
        })
        .collect::<Result<Parents>>()
}

/// Traverses a VDev tree and returns back it's paths
fn get_vdev_paths(vdev: &libzfs_types::VDev) -> OrdSet<PathBuf> {
    match vdev {
        libzfs_types::VDev::Disk { path, .. } => im::ordset![path.clone()],
        libzfs_types::VDev::File { .. } => im::ordset![],
        libzfs_types::VDev::Mirror { children, .. }
        | libzfs_types::VDev::RaidZ { children, .. }
        | libzfs_types::VDev::Replacing { children, .. } => {
            children.into_iter().flat_map(get_vdev_paths).collect()
        }
        libzfs_types::VDev::Root {
            children,
            spares,
            cache,
            ..
        } => vec![children, spares, cache]
            .into_iter()
            .flatten()
            .flat_map(get_vdev_paths)
            .collect(),
    }
}

fn get_fs_and_mount<'a>(
    x: &'a UEvent,
    ys: &HashSet<Mount>,
) -> (Option<String>, devices::MountPath) {
    let mount = ys.iter().find(
        |Mount {
             source: BdevPath(s),
             ..
         }| { x.paths.iter().any(|x| x == s) },
    );

    match mount {
        Some(Mount {
            fs_type: FsType(f),
            target: MountPoint(m),
            ..
        }) => (Some(f.clone()), Some(m.clone())),
        None => (x.fs_type.clone(), None),
    }
}

fn get_zfs_mount_and_fs(name: &str, ys: &HashSet<Mount>) -> (Option<PathBuf>, Option<String>) {
    let opt = ys
        .iter()
        .find(
            |Mount {
                 source: BdevPath(s),
                 ..
             }| { s.to_string_lossy() == name },
        )
        .map(
            |Mount {
                 target: MountPoint(m),
                 fs_type: FsType(f),
                 ..
             }| { (m.clone(), f.clone()) },
        );

    match opt {
        Some((x, y)) => (Some(x), Some(y)),
        None => (None, None),
    }
}

fn create_md(
    x: &UEvent,
    filesystem_type: Option<String>,
    mount_path: devices::MountPath,
    parents: Parents,
) -> Result<Device> {
    Ok(Device::MdRaid(devices::MdRaid {
        serial: x.get_serial()?,
        paths: x.paths.clone(),
        filesystem_type,
        filesystem_label: x.fs_label.clone(),
        filesystem_uuid: x.fs_uuid.clone(),
        mount_path,
        major: x.major.clone(),
        minor: x.minor.clone(),
        size: x.size.ok_or_else(|| error::none_error("Expected size"))?,
        parents,
        uuid: x
            .md_uuid
            .clone()
            .ok_or_else(|| error::none_error("Expected md_uuid"))?,
    }))
}

fn create_mpath(
    x: &UEvent,
    filesystem_type: Option<String>,
    mount_path: devices::MountPath,
    parents: Parents,
) -> Result<Device> {
    Ok(Device::Mpath(devices::Mpath {
        serial: x.get_serial()?,
        size: x.size.ok_or_else(|| error::none_error("Expected size"))?,
        major: x.major.clone(),
        minor: x.minor.clone(),
        paths: x.paths.clone(),
        parents,
        filesystem_type,
        filesystem_label: x.fs_label.clone(),
        filesystem_uuid: x.fs_uuid.clone(),
        devpath: x.devpath.clone(),
        mount_path,
    }))
}

fn create_partition(
    x: &UEvent,
    filesystem_type: Option<String>,
    mount_path: devices::MountPath,
    parents: Parents,
) -> Result<Device> {
    Ok(Device::Partition(devices::Partition {
        serial: x.get_serial()?,
        partition_number: x
            .part_entry_number
            .ok_or_else(|| error::none_error("Expected part_entry_number"))?,
        parents,
        devpath: x.devpath.clone(),
        major: x.major.clone(),
        minor: x.minor.clone(),
        size: x.size.ok_or_else(|| error::none_error("Expected size"))?,
        paths: x.paths.clone(),
        filesystem_type,
        filesystem_label: x.fs_label.clone(),
        filesystem_uuid: x.fs_uuid.clone(),
        mount_path,
    }))
}

fn create_vg(x: &UEvent, parents: Parents) -> Result<Device> {
    Ok(Device::VolumeGroup(devices::VolumeGroup {
        name: x
            .dm_vg_name
            .clone()
            .ok_or_else(|| error::none_error("Expected dm_vg_name"))?,
        parents,
        size: x
            .dm_vg_size
            .ok_or_else(|| error::none_error("Expected Size"))?,
        serial: x
            .vg_uuid
            .clone()
            .map(Serial)
            .ok_or_else(|| error::none_error("Expected vg_uuid"))?,
    }))
}

fn create_lv(
    x: &UEvent,
    filesystem_type: Option<String>,
    mount_path: devices::MountPath,
) -> Result<Device> {
    Ok(Device::LogicalVolume(devices::LogicalVolume {
        serial: x
            .lv_uuid
            .clone()
            .map(Serial)
            .ok_or_else(|| error::none_error("Expected lv_uuid"))?,
        name: x
            .dm_lv_name
            .clone()
            .ok_or_else(|| error::none_error("Expected dm_lv_name"))?,
        devpath: x.devpath.clone(),
        size: x.size.ok_or_else(|| error::none_error("Expected size"))?,
        parent: (
            devices::DeviceType::VolumeGroup,
            x.vg_uuid
                .clone()
                .map(Serial)
                .ok_or_else(|| error::none_error("Expected vg_uuid"))?,
        ),
        major: x.major.clone(),
        minor: x.minor.clone(),
        paths: x.paths.clone(),
        mount_path,
        filesystem_type,
        filesystem_label: x.fs_label.clone(),
        filesystem_uuid: x.lv_uuid.clone(),
    }))
}

fn create_scsi(
    x: &UEvent,
    filesystem_type: Option<String>,
    mount_path: devices::MountPath,
) -> Result<Device> {
    Ok(Device::ScsiDevice(devices::ScsiDevice {
        serial: x.get_serial()?,
        devpath: x.devpath.clone(),
        major: x.major.clone(),
        minor: x.minor.clone(),
        size: x.size.ok_or_else(|| error::none_error("Expected size"))?,
        filesystem_type,
        filesystem_label: x.fs_label.clone(),
        filesystem_uuid: x.fs_uuid.clone(),
        paths: x.paths.clone(),
        mount_path,
    }))
}

fn create_pool(
    x: &libzfs_types::Pool,
    mount_path: devices::MountPath,
    filesystem_type: Option<String>,
    parents: Parents,
) -> Result<Device> {
    Ok(Device::Zpool(devices::Zpool {
        serial: Serial(x.guid.to_string()),
        health: x.health.clone(),
        name: x.name.clone(),
        props: x.props.clone(),
        state: x.state.clone(),
        parents,
        size: x.size.parse()?,
        paths: im::ordset![x.name.clone().into()],
        filesystem_type,
        filesystem_label: None,
        mount_path,
    }))
}

fn create_dataset(
    x: &libzfs_types::Dataset,
    pool_serial: Serial,
    pool_size: i64,
    mount_path: devices::MountPath,
    filesystem_type: Option<String>,
) -> Result<Device> {
    Ok(Device::Dataset(devices::Dataset {
        name: x.name.clone(),
        pool_serial,
        serial: Serial(x.guid.clone()),
        kind: x.kind.clone(),
        props: x.props.clone(),
        size: pool_size,
        paths: im::ordset![x.name.clone().into()],
        mount_path,
        filesystem_type,
        filesystem_label: x
            .props
            .iter()
            .find(|x| x.name == "lustre:svname")
            .map(|x| x.value.clone()),
        filesystem_uuid: Some(x.guid.clone()),
    }))
}

fn create_devices<'a>(
    xs: &'a state::UEvents,
    ys: &'a state::ZedEvents,
    zs: &'a HashSet<Mount>,
) -> Result<HashSet<Device>> {
    let devices =
        xs.values()
            .filter(|y| y.keep_usable())
            .map(|x: &UEvent| -> Result<Vec<Device>> {
                if x.is_dm() {
                    let parents = get_parents(xs, |y| {
                        find_by_major_minor(&x.dm_slave_mms, &y.major, &y.minor)
                    })?;

                    let (fs, mount) = get_fs_and_mount(x, zs);

                    let vg = create_vg(x, parents)?;

                    Ok(vec![vg, create_lv(x, fs, mount)?])
                } else if x.is_mdraid() {
                    let parents = get_parents(xs, |y| do_paths_intersect(&x.md_devs, &y.paths))?;

                    let (fs, mount) = get_fs_and_mount(x, zs);

                    Ok(vec![create_md(x, fs, mount, parents)?])
                } else if x.is_mpath() {
                    let parents = get_parents(xs, |y| {
                        find_by_major_minor(&x.dm_slave_mms, &y.major, &y.minor)
                    })?;

                    let (fs, mount) = get_fs_and_mount(x, zs);

                    Ok(vec![create_mpath(x, fs, mount, parents)?])
                } else if x.is_partition() {
                    let (fs, mount) = get_fs_and_mount(x, zs);

                    let parents = get_parents(xs, |y| match x.part_entry_mm {
                        Some(ref x) if x == &format_major_minor(&y.major, &y.minor) => true,
                        _ => false,
                    })?;

                    Ok(vec![create_partition(x, fs, mount, parents)?])
                } else {
                    let (fs, mount) = get_fs_and_mount(x, zs);

                    Ok(vec![create_scsi(x, fs, mount)?])
                }
            });

    let pools = ys.values().map(|p| -> Result<Vec<Device>> {
        let mut ds: Vec<Device> = p
            .datasets
            .iter()
            .map(|d| {
                let (mount_point, filesystem_type) = get_zfs_mount_and_fs(&d.name, zs);

                create_dataset(
                    &d,
                    Serial(p.guid.to_string()),
                    p.size.parse()?,
                    mount_point,
                    filesystem_type,
                )
            })
            .collect::<Result<Vec<Device>>>()?;

        let (mount_point, filesystem_type) = get_zfs_mount_and_fs(&p.name, zs);

        let paths = self::get_vdev_paths(&p.vdev);

        let parents = get_parents(xs, |y| do_paths_intersect(&y.paths, &paths))?;

        ds.push(create_pool(p, mount_point, filesystem_type, parents)?);

        Ok(ds)
    });

    let devices = devices.chain(pools).collect::<Result<Vec<Vec<Device>>>>()?;

    Ok(devices.into_iter().flat_map(|x| x).collect())
}

pub struct State {
    conns: Vec<connections::Tx>,
    state: state::State,
}

impl State {
    fn new() -> Self {
        State {
            conns: vec![],
            state: state::State::new(),
        }
    }
}

pub fn handler() -> (
    UnboundedSender<(Command, connections::Tx)>,
    impl Future<Item = State, Error = error::Error>,
) {
    let (tx, rx) = mpsc::unbounded();

    let fut = rx
        .map_err(|_| {
            error::Error::Io(io::Error::new(
                io::ErrorKind::Other,
                "Could not consume rx stream",
            ))
        })
        .fold(
            State::new(),
            |State {
                 mut conns,
                 state:
                     state::State {
                         uevents,
                         zed_events,
                         local_mounts,
                     },
             }: State,
             (cmd, connections_tx): (Command, connections::Tx)|
             -> Result<State> {
                conns.push(connections_tx);

                let (uevents, local_mounts, zed_events) = match cmd {
                    Command::UdevCommand(x) => {
                        let uevents = update_udev(&uevents, x);
                        (uevents, local_mounts, zed_events)
                    }
                    Command::MountCommand(x) => {
                        let local_mounts = update_mount(local_mounts, x);
                        (uevents, local_mounts, zed_events)
                    }
                    Command::PoolCommand(x) => {
                        let zed_events = update_zed_events(zed_events, x)?;
                        (uevents, local_mounts, zed_events)
                    }
                    _ => (uevents, local_mounts, zed_events),
                };

                {
                    let dev_list = create_devices(&uevents, &zed_events, &local_mounts)?;

                    let v = serde_json::to_string(&dev_list)?;
                    let b = bytes::BytesMut::from(v + "\n");
                    let b = b.freeze();

                    conns = conns
                        .into_iter()
                        .filter(|c| c.unbounded_send(b.clone()).is_ok())
                        .collect();
                };

                Ok(State {
                    conns,
                    state: state::State {
                        uevents,
                        local_mounts,
                        zed_events,
                    },
                })
            },
        );

    (tx, fut)
}
