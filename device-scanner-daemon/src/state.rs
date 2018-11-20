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

use im::{hashset, vector, HashSet, Vector};
use serde_json;
use std::{io, iter::IntoIterator, path::PathBuf};
use tokio::prelude::*;

use connections;
use device_types::{
    devices::Device,
    mount::{BdevPath, FsType, Mount, MountPoint},
    state,
    uevent::UEvent,
    Command,
};
use iml_device_fns::{find_by_major_minor, format_major_minor, get_vdev_paths};

use reducers::{mount::update_mount, udev::update_udev, zed::update_zed_events};

use error::{self, Result};

/// Filter out any devices that are not suitable for mounting a filesystem.
fn keep_usable(x: &UEvent) -> bool {
    x.size != Some(0) && x.size.is_some() && x.read_only != Some(true) && x.bios_boot != Some(true)
}

fn is_mpath(x: &UEvent) -> bool {
    x.is_mpath == Some(true)
}

fn is_dm(x: &UEvent) -> bool {
    [&x.lv_uuid, &x.vg_uuid, &x.dm_lv_name]
        .iter()
        .all(|x| x.is_some())
}

fn is_partition(x: &UEvent) -> bool {
    x.part_entry_mm.is_some()
}

fn is_mdraid(x: &UEvent) -> bool {
    x.md_uuid.is_some()
}

fn find_mount<'a>(xs: &HashSet<PathBuf>, ys: &'a HashSet<Mount>) -> Option<&'a Mount> {
    ys.iter().find(
        |Mount {
             source: BdevPath(s),
             ..
         }| { xs.iter().any(|x| x == s) },
    )
}

fn get_vgs(b: &Buckets, major: &str, minor: &str) -> Result<HashSet<Device>> {
    b.dms
        .iter()
        .filter(|&x| find_by_major_minor(&x.dm_slave_mms, major, minor))
        .map(|x| {
            Ok(Device::VolumeGroup {
                name: x
                    .dm_vg_name
                    .clone()
                    .ok_or_else(|| error::none_error("Expected dm_vg_name"))?,
                children: hashset![],
                major_minors: x.dm_slave_mms.clone(),
                size: x
                    .dm_vg_size
                    .ok_or_else(|| error::none_error("Expected Size"))?,
                uuid: x
                    .vg_uuid
                    .clone()
                    .ok_or_else(|| error::none_error("Expected vg_uuid"))?,
            })
        }).collect()
}

fn get_partitions(
    b: &Buckets,
    ys: &HashSet<Mount>,
    major: &str,
    minor: &str,
) -> Result<HashSet<Device>> {
    b.partitions
        .iter()
        .filter(|&x| match x.part_entry_mm {
            Some(ref p) => p == &format_major_minor(major, minor),
            None => false,
        }).map(|x| {
            let mount = find_mount(&x.paths, ys);

            let (filesystem_type, mount_path) = match mount {
                Some(Mount {
                    fs_type: FsType(f),
                    target: MountPoint(m),
                    ..
                }) => (Some(f.clone()), Some(m.clone())),
                None => (x.fs_type.clone(), None),
            };

            Ok(Device::Partition {
                partition_number: x
                    .part_entry_number
                    .ok_or_else(|| error::none_error("Expected part_entry_number"))?,
                devpath: x.devpath.clone(),
                major: x.major.clone(),
                minor: x.minor.clone(),
                size: x.size.ok_or_else(|| error::none_error("Expected size"))?,
                paths: x.paths.clone(),
                filesystem_type,
                children: hashset![],
                mount_path,
            })
        }).collect()
}

fn get_lvs(b: &Buckets, ys: &HashSet<Mount>, uuid: &str) -> Result<HashSet<Device>> {
    b.dms
        .iter()
        .filter(|&x| match x.vg_uuid {
            Some(ref p) => p == uuid,
            None => false,
        }).map(|x| {
            let mount = find_mount(&x.paths, ys);

            let (filesystem_type, mount_path) = match mount {
                Some(Mount {
                    fs_type: FsType(f),
                    target: MountPoint(m),
                    ..
                }) => (Some(f.clone()), Some(m.clone())),
                None => (x.fs_type.clone(), None),
            };

            Ok(Device::LogicalVolume {
                name: x
                    .dm_lv_name
                    .clone()
                    .ok_or_else(|| error::none_error("Expected dm_lv_name"))?,
                devpath: x.devpath.clone(),
                uuid: x
                    .lv_uuid
                    .clone()
                    .ok_or_else(|| error::none_error("Expected lv_uuid"))?,
                size: x.size.ok_or_else(|| error::none_error("Expected size"))?,
                major: x.major.clone(),
                minor: x.minor.clone(),
                paths: x.paths.clone(),
                mount_path,
                filesystem_type,
                children: hashset![],
            })
        }).collect()
}

fn get_scsis(b: &Buckets, ys: &HashSet<Mount>) -> Result<HashSet<Device>> {
    b.rest
        .iter()
        .map(|x| {
            let mount = find_mount(&x.paths, ys);

            let (filesystem_type, mount_path) = match mount {
                Some(Mount {
                    fs_type: FsType(f),
                    target: MountPoint(m),
                    ..
                }) => (Some(f.clone()), Some(m.clone())),
                None => (x.fs_type.clone(), None),
            };

            Ok(Device::ScsiDevice {
                serial: x
                    .scsi83
                    .clone()
                    .ok_or_else(|| error::none_error("Expected serial"))?,
                devpath: x.devpath.clone(),
                major: x.major.clone(),
                minor: x.minor.clone(),
                size: x.size.ok_or_else(|| error::none_error("Expected size"))?,
                filesystem_type,
                paths: x.paths.clone(),
                children: hashset![],
                mount_path,
            })
        }).collect()
}

fn get_mpaths(
    b: &Buckets,
    ys: &HashSet<Mount>,
    major: &str,
    minor: &str,
) -> Result<HashSet<Device>> {
    b.mpaths
        .iter()
        .filter(|&x| find_by_major_minor(&x.dm_slave_mms, major, minor))
        .map(|x| {
            let mount = find_mount(&x.paths, ys);

            let (filesystem_type, mount_path) = match mount {
                Some(Mount {
                    fs_type: FsType(f),
                    target: MountPoint(m),
                    ..
                }) => (Some(f.clone()), Some(m.clone())),
                None => (x.fs_type.clone(), None),
            };

            Ok(Device::Mpath {
                serial: x
                    .scsi83
                    .clone()
                    .ok_or_else(|| error::none_error("Expected serial"))?,
                size: x.size.ok_or_else(|| error::none_error("Expected size"))?,
                major: x.major.clone(),
                minor: x.minor.clone(),
                paths: x.paths.clone(),
                filesystem_type,
                children: hashset![],
                devpath: x.devpath.clone(),
                mount_path,
            })
        }).collect()
}

fn get_mds(b: &Buckets, ys: &HashSet<Mount>, paths: &HashSet<PathBuf>) -> Result<HashSet<Device>> {
    b.mds
        .iter()
        .filter(|&x| paths.clone().intersection(x.md_devs.clone()).is_empty())
        .map(|x| {
            let mount = find_mount(&x.paths, ys);

            let (filesystem_type, mount_path) = match mount {
                Some(Mount {
                    fs_type: FsType(f),
                    target: MountPoint(m),
                    ..
                }) => (Some(f.clone()), Some(m.clone())),
                None => (x.fs_type.clone(), None),
            };

            Ok(Device::MdRaid {
                paths: x.paths.clone(),
                filesystem_type,
                mount_path,
                major: x.major.clone(),
                minor: x.minor.clone(),
                size: x.size.ok_or_else(|| error::none_error("Expected size"))?,
                parent_paths: x.md_devs.clone(),
                children: hashset![],
                uuid: x
                    .md_uuid
                    .clone()
                    .ok_or_else(|| error::none_error("Expected md_uuid"))?,
            })
        }).collect()
}

fn get_pools(
    b: &Buckets,
    ys: &HashSet<Mount>,
    paths: &HashSet<PathBuf>,
) -> Result<HashSet<Device>> {
    b.pools
        .iter()
        .filter(|&x| {
            let vdev_paths = get_vdev_paths(x.vdev.clone());

            !paths.clone().intersection(vdev_paths).is_empty()
        }).map(|x| {
            Ok(Device::Zpool {
                guid: x.guid,
                health: x.health.clone(),
                name: x.name.clone(),
                props: x.props.clone(),
                state: x.state.clone(),
                vdev: x.vdev.clone(),
                size: x.size.parse()?,
                children: hashset![],
            })
        }).collect()
}

fn get_datasets(b: &Buckets, guid: u64) -> Result<HashSet<Device>> {
    let ds = b
        .pools
        .iter()
        .find(|p| p.guid == guid)
        .map(|p| &p.datasets)
        .ok_or_else(|| {
            error::none_error(format!(
                "Could not find pool with guid: {} in buckets",
                guid
            ))
        })?;

    ds.iter()
        .map(|x| {
            Ok(Device::Dataset {
                name: x.name.clone(),
                guid: x.guid.clone(),
                kind: x.kind.clone(),
                props: x.props.clone(),
            })
        }).collect()
}

fn build_device_graph<'a>(ptr: &mut Device, b: &Buckets<'a>, ys: &HashSet<Mount>) -> Result<()> {
    match ptr {
        Device::Root { children, .. } => {
            let ss = get_scsis(&b, &ys)?;

            for mut x in ss {
                build_device_graph(&mut x, b, ys)?;

                children.insert(x);
            }

            Ok(())
        }
        Device::Mpath {
            children,
            major,
            minor,
            paths,
            ..
        } => {
            let vs = get_vgs(&b, major, minor)?;

            let ps = get_partitions(&b, &ys, major, minor)?;

            let mds = get_mds(&b, &ys, &paths)?;

            let pools = get_pools(&b, &ys, &paths)?;

            for mut x in HashSet::unions(vec![vs, ps, mds, pools]) {
                build_device_graph(&mut x, b, ys)?;

                children.insert(x);
            }

            Ok(())
        }
        Device::ScsiDevice {
            children,
            paths,
            major,
            minor,
            ..
        }
        | Device::Partition {
            children,
            paths,
            major,
            minor,
            ..
        } => {
            let xs = get_partitions(&b, &ys, &major, &minor)?;

            // This should only be present for scsi devs
            let ms = get_mpaths(&b, &ys, major, minor)?;

            let vs = get_vgs(b, major, minor)?;

            let mds = get_mds(&b, &ys, &paths)?;

            let pools = get_pools(&b, &ys, &paths)?;

            for mut x in HashSet::unions(vec![xs, ms, vs, mds, pools]) {
                build_device_graph(&mut x, b, ys)?;

                children.insert(x);
            }

            Ok(())
        }
        Device::VolumeGroup { children, uuid, .. } => {
            let lvs = get_lvs(&b, &ys, &uuid)?;

            for mut x in lvs {
                build_device_graph(&mut x, b, ys)?;

                children.insert(x);
            }

            Ok(())
        }
        Device::LogicalVolume {
            major,
            minor,
            children,
            paths,
            ..
        } => {
            let ps = get_partitions(&b, &ys, &major, &minor)?;

            let pools = get_pools(&b, &ys, &paths)?;

            for mut x in HashSet::unions(vec![ps, pools]) {
                build_device_graph(&mut x, b, ys)?;

                children.insert(x);
            }

            Ok(())
        }
        Device::MdRaid {
            major,
            minor,
            children,
            paths,
            ..
        } => {
            let vs = get_vgs(&b, &major, &minor)?;

            let ps = get_partitions(&b, &ys, major, minor)?;

            let mds = get_mds(&b, &ys, &paths)?;

            let pools = get_pools(&b, &ys, &paths)?;

            for mut x in HashSet::unions(vec![vs, ps, mds, pools]) {
                build_device_graph(&mut x, b, ys)?;

                children.insert(x);
            }

            Ok(())
        }
        Device::Zpool { guid, children, .. } => {
            let ds = get_datasets(&b, *guid)?;

            for mut x in ds {
                build_device_graph(&mut x, b, ys)?;

                children.insert(x);
            }

            Ok(())
        }
        Device::Dataset { .. } => Ok(()),
    }
}

#[derive(Debug)]
struct Buckets<'a> {
    dms: Vector<&'a UEvent>,
    mds: Vector<&'a UEvent>,
    mpaths: Vector<&'a UEvent>,
    partitions: Vector<&'a UEvent>,
    pools: Vector<&'a libzfs_types::Pool>,
    rest: Vector<&'a UEvent>,
}

fn bucket_devices<'a>(xs: &Vector<&'a UEvent>, ys: &'a state::ZedEvents) -> Buckets<'a> {
    let buckets = Buckets {
        dms: vector![],
        mds: vector![],
        mpaths: vector![],
        partitions: vector![],
        pools: vector![],
        rest: vector![],
    };

    let mut buckets = xs.iter().fold(buckets, |mut acc, x| {
        if is_dm(&x) {
            acc.dms.push_back(x)
        } else if is_mdraid(&x) {
            acc.mds.push_back(x)
        } else if is_mpath(&x) {
            acc.mpaths.push_back(x)
        } else if is_partition(&x) {
            acc.partitions.push_back(x)
        } else {
            acc.rest.push_back(x)
        }

        acc
    });

    buckets.pools = ys.values().collect();

    buckets
}

fn build_device_list(xs: &mut state::UEvents) -> Vector<&UEvent> {
    xs.values().filter(|y| keep_usable(y)).collect()
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
        }).fold(
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

                let (mut uevents, local_mounts, zed_events) = match cmd {
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
                    let dev_list = build_device_list(&mut uevents);
                    let dev_list = bucket_devices(&dev_list, &zed_events);

                    let mut root = Device::Root {
                        children: hashset![],
                    };

                    build_device_graph(&mut root, &dev_list, &local_mounts)?;

                    let v = serde_json::to_string(&root)?;
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
