//! Handles Updates of state
//!
//! `device-scanner` uses a persistent streaming strategy
//! where Unix domain sockets can connect and be fed device-graph changes as they occur.
//! This module is responsible for internally storing the current state and building the next device-graph
//! after each "tick" (an incoming device event).

use futures::future::Future;
use futures::sync::mpsc::{self, UnboundedSender};

use im::{HashSet, Vector};
use serde_json;
use std::{io, path::PathBuf, result};
use tokio::prelude::*;

use connections;
use device_types::{
    devices::Device,
    mount::{BdevPath, FsType, Mount, MountCommand, MountPoint},
    state,
    udev::UdevCommand,
    uevent::UEvent,
    Command,
};

use error;

type Result<T> = result::Result<T, error::Error>;

/// Mutably updates the Udev portion of the device map in response to `UdevCommand`s.
fn update_udev(uevents: &mut state::UEvents, cmd: UdevCommand) {
    match cmd {
        UdevCommand::Add(x) | UdevCommand::Change(x) => uevents.insert(x.devpath.clone(), x),
        UdevCommand::Remove(x) => uevents.remove(&x.devpath),
    };
}

/// Mutably updates the Mount portion of the device map in response to `MountCommand`s.
fn update_mount(local_mounts: &mut HashSet<Mount>, cmd: MountCommand) {
    match cmd {
        MountCommand::AddMount(target, source, fstype, opts) => {
            local_mounts.insert(Mount::new(target, source, fstype, opts))
        }
        MountCommand::RemoveMount(target, source, fstype, opts) => {
            local_mounts.remove(&Mount::new(target, source, fstype, opts))
        }
        MountCommand::ReplaceMount(target, source, fstype, opts, old_ops) => {
            local_mounts.remove(&Mount::new(
                target.clone(),
                source.clone(),
                fstype.clone(),
                old_ops,
            ));

            local_mounts.insert(Mount::new(target, source, fstype, opts))
        }
        MountCommand::MoveMount(target, source, fstype, opts, old_target) => {
            local_mounts.remove(&Mount::new(
                old_target,
                source.clone(),
                fstype.clone(),
                opts.clone(),
            ));

            local_mounts.insert(Mount::new(target, source, fstype, opts))
        }
    };
}

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

fn format_major_minor(major: &str, minor: &str) -> String {
    format!("{}:{}", major, minor)
}

fn find_by_major_minor(xs: &Vector<String>, major: &str, minor: &str) -> bool {
    xs.contains(&format_major_minor(major, minor))
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
                children: hashset![],
                uuid: x
                    .md_uuid
                    .clone()
                    .ok_or_else(|| error::none_error("Expected md_uuid"))?,
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

            for mut x in HashSet::unions(vec![vs, ps, mds]) {
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

            for mut x in HashSet::unions(vec![xs, ms, vs, mds]) {
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
            ..
        } => {
            let ps = get_partitions(&b, &ys, &major, &minor)?;

            for mut x in ps {
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

            for mut x in HashSet::unions(vec![vs, ps, mds]) {
                build_device_graph(&mut x, b, ys)?;

                children.insert(x);
            }

            Ok(())
        }
        Device::Zpool { .. } => Ok(()),
        Device::Dataset { .. } => Ok(()),
    }
}

#[derive(Debug)]
struct Buckets<'a> {
    dms: Vector<&'a UEvent>,
    mds: Vector<&'a UEvent>,
    mpaths: Vector<&'a UEvent>,
    partitions: Vector<&'a UEvent>,
    rest: Vector<&'a UEvent>,
}

fn bucket_devices<'a>(xs: &Vector<&'a UEvent>) -> Buckets<'a> {
    let buckets = Buckets {
        dms: vector![],
        mds: vector![],
        mpaths: vector![],
        partitions: vector![],
        rest: vector![],
    };

    xs.iter().fold(buckets, |mut acc, x| {
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
    })
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
                         mut uevents,
                         mut local_mounts,
                     },
             }: State,
             (cmd, connections_tx): (Command, connections::Tx)|
             -> Result<State> {
                println!("got new state");

                conns.push(connections_tx);

                match cmd {
                    Command::UdevCommand(x) => update_udev(&mut uevents, x),
                    Command::MountCommand(x) => update_mount(&mut local_mounts, x),
                    _ => (),
                };

                {
                    let dev_list = build_device_list(&mut uevents);
                    let dev_list = bucket_devices(&dev_list);

                    let mut root = Device::Root {
                        children: hashset![],
                    };

                    build_device_graph(&mut root, &dev_list, &local_mounts)?;

                    let v = serde_json::to_vec(&root)?;
                    // Using bytes here allows us
                    let b = bytes::BytesMut::from(v);
                    let b = b.freeze();

                    println!("finished with state");

                    conns = conns
                        .into_iter()
                        .filter(|c| c.unbounded_send(b.clone()).is_ok())
                        .collect();

                    println!("conns size {}", conns.len());
                };

                Ok(State {
                    conns,
                    state: state::State {
                        uevents,
                        local_mounts,
                    },
                })
            },
        );

    (tx, fut)
}

#[cfg(test)]
mod tests {
    use super::*;
    use device_types::{
        mount::{self, MountCommand},
        udev::UdevCommand,
        uevent::UEvent,
    };

    fn create_path_buf(s: &str) -> PathBuf {
        let mut p = PathBuf::new();
        p.push(s);

        p
    }

    #[test]
    fn test_udev_update() {
        let ev = UEvent {
            major: "253".to_string(),
            minor: "20".to_string(),
            seqnum: 3547,
            paths: hashset![
                create_path_buf(
                    "/dev/disk/by-id/dm-uuid-part1-mpath-3600140550e41a841db244a992c31e7df"
                ),
                create_path_buf("/dev/mapper/mpathd1"),
                create_path_buf("/dev/disk/by-uuid/b4550256-cf48-4013-8363-bfee5f52da12"),
                create_path_buf("/dev/disk/by-partuuid/d643e32f-b6b9-4863-af8f-8950376e28da"),
                create_path_buf("/dev/dm-20"),
                create_path_buf("/dev/disk/by-id/dm-name-mpathd1")
            ],
            devname: create_path_buf("/dev/dm-20"),
            devpath: create_path_buf("/devices/virtual/block/dm-20"),
            devtype: "disk".to_string(),
            vendor: None,
            model: None,
            serial: None,
            fs_type: Some("ext4".to_string()),
            fs_usage: Some("filesystem".to_string()),
            fs_uuid: Some("b4550256-cf48-4013-8363-bfee5f52da12".to_string()),
            part_entry_number: Some(1),
            part_entry_mm: Some("253:13".to_string()),
            size: Some(100_651_008),
            scsi80: Some(
                "SLIO-ORG ost12           50e41a84-1db2-44a9-92c3-1e7dfad48fce".to_string(),
            ),
            scsi83: Some("3600140550e41a841db244a992c31e7df".to_string()),
            read_only: Some(false),
            bios_boot: None,
            zfs_reserved: None,
            is_mpath: None,
            dm_slave_mms: vector!["253:13".to_string()],
            dm_vg_size: Some(0),
            md_devs: hashset![],
            dm_multipath_devpath: None,
            dm_name: Some("mpathd1".to_string()),
            dm_lv_name: None,
            lv_uuid: None,
            dm_vg_name: None,
            vg_uuid: None,
            md_uuid: None,
        };

        let mut ev2 = ev.clone();
        ev2.size = Some(100_651_001);

        let mut uevents = hashmap!{ev.devpath.clone() => ev.clone()};

        let add_cmd = UdevCommand::Add(ev.clone());

        update_udev(&mut uevents, add_cmd);

        assert_eq!(hashmap!{ev.devpath.clone() => ev.clone()}, uevents);

        let change_cmd = UdevCommand::Change(ev2.clone());

        update_udev(&mut uevents, change_cmd);

        assert_eq!(hashmap!{ev.devpath.clone() => ev2.clone()}, uevents);

        let remove_cmd = UdevCommand::Remove(ev2.clone());

        update_udev(&mut uevents, remove_cmd);

        assert_eq!(hashmap!{}, uevents);
    }

    #[test]
    fn test_mount_update() {
        let mut mounts = hashset!();

        let add_cmd = MountCommand::AddMount(
            MountPoint(create_path_buf("/mnt/part1")),
            BdevPath(create_path_buf("/dev/sde1")),
            FsType("ext4".to_string()),
            mount::MountOpts("rw,relatime,data=ordered".to_string()),
        );

        update_mount(&mut mounts, add_cmd);

        assert_eq!(
            hashset!(Mount {
                target: MountPoint(create_path_buf("/mnt/part1")),
                source: BdevPath(create_path_buf("/dev/sde1")),
                fs_type: FsType("ext4".to_string()),
                opts: mount::MountOpts("rw,relatime,data=ordered".to_string())
            }),
            mounts
        );

        let mv_cmd = MountCommand::MoveMount(
            MountPoint(create_path_buf("/mnt/part3")),
            BdevPath(create_path_buf("/dev/sde1")),
            FsType("ext4".to_string()),
            mount::MountOpts("rw,relatime,data=ordered".to_string()),
            MountPoint(create_path_buf("/mnt/part1")),
        );

        update_mount(&mut mounts, mv_cmd);

        assert_eq!(
            hashset!(Mount {
                target: MountPoint(create_path_buf("/mnt/part3")),
                source: BdevPath(create_path_buf("/dev/sde1")),
                fs_type: FsType("ext4".to_string()),
                opts: mount::MountOpts("rw,relatime,data=ordered".to_string())
            }),
            mounts
        );

        let replace_cmd = MountCommand::ReplaceMount(
            MountPoint(create_path_buf("/mnt/part3")),
            BdevPath(create_path_buf("/dev/sde1")),
            FsType("ext4".to_string()),
            mount::MountOpts("r,relatime,data=ordered".to_string()),
            mount::MountOpts("rw,relatime,data=ordered".to_string()),
        );

        update_mount(&mut mounts, replace_cmd);

        assert_eq!(
            hashset!(Mount {
                target: MountPoint(create_path_buf("/mnt/part3")),
                source: BdevPath(create_path_buf("/dev/sde1")),
                fs_type: FsType("ext4".to_string()),
                opts: mount::MountOpts("r,relatime,data=ordered".to_string())
            }),
            mounts
        );

        let rm_cmd = MountCommand::RemoveMount(
            MountPoint(create_path_buf("/mnt/part3")),
            BdevPath(create_path_buf("/dev/sde1")),
            FsType("ext4".to_string()),
            mount::MountOpts("r,relatime,data=ordered".to_string()),
        );

        update_mount(&mut mounts, rm_cmd);

        assert_eq!(hashset!(), mounts);
    }

}
