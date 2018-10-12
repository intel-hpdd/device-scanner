use futures::future::Future;
use futures::sync::mpsc::{self, UnboundedSender};

use connections;
use device_types::{
    devices::Device,
    mount::{BdevPath, FsType, Mount, MountCommand, MountPoint},
    state::{State, UEvents},
    udev::UdevCommand,
    uevent::UEvent,
    Command,
};
use im::{HashSet, Vector};
use serde_json;
use std::path::PathBuf;
use tokio::{net::UnixStream, prelude::*};

fn update_udev(uevents: &mut UEvents, cmd: UdevCommand) {
    match cmd {
        UdevCommand::Add(x) | UdevCommand::Change(x) => uevents.insert(x.devpath.clone(), x),
        UdevCommand::Remove(x) => uevents.remove(&x.devpath),
    };
}

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

fn keep_usable(x: &UEvent) -> bool {
    x.size != Some(0) && x.size.is_some() && x.read_only != Some(true) && x.bios_boot != Some(true)
}

fn is_mpath(x: &UEvent) -> bool {
    x.is_mpath == Some(true)
}

fn is_dm(x: &UEvent) -> bool {
    let xs: Option<Vector<String>> =
        vector![x.lv_uuid.clone(), x.vg_uuid.clone(), x.dm_lv_name.clone()]
            .into_iter()
            .collect();

    xs.is_some()
}

fn is_partition(x: &UEvent) -> bool {
    x.parent.is_some()
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

fn find_by_major_minor(xs: &Vector<String>, major: &str, minor: &str) -> bool {
    xs.contains(&format!("{}:{}", major, minor))
}

fn intersections<I, T>(i: I) -> HashSet<T>
where
    I: IntoIterator<Item = HashSet<T>>,
    T: ::std::hash::Hash + Eq + Clone,
{
    i.into_iter()
        .fold(HashSet::default(), |a, b| a.intersection(b))
}

fn build_device_graph<'a>(ptr: &mut Device, b: &Buckets<'a>, ys: &HashSet<Mount>) {
    fn get_vgs(b: &Buckets, major: &str, minor: &str) -> HashSet<Device> {
        b.dms
            .iter()
            .filter(|&x| find_by_major_minor(&x.dm_slave_mms, major, minor))
            .map(|x| Device::VolumeGroup {
                name: x.dm_vg_name.clone().expect("Expected dm_vg_name"),
                children: hashset![],
                size: x.dm_vg_size.expect("Expected size"),
                uuid: x.vg_uuid.clone().expect("Expected vg_uuid"),
            }).collect()
    }

    fn get_partitions(b: &Buckets, ys: &HashSet<Mount>, devpath: &PathBuf) -> HashSet<Device> {
        b.partitions
            .iter()
            .filter(|&x| match x.parent {
                Some(ref p) => p == devpath,
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

                Device::Partition {
                    partition_number: x.part_entry_number.expect("Expected part_entry_number"),
                    devpath: x.devpath.clone(),
                    major: x.major.clone(),
                    minor: x.minor.clone(),
                    size: x.size.expect("Expected size"),
                    paths: x.paths.clone(),
                    filesystem_type,
                    children: hashset![],
                    mount_path,
                }
            }).collect()
    }

    match ptr {
        Device::Root { children, .. } => {
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

                    Device::ScsiDevice {
                        serial: x.scsi83.clone().expect("Expected serial"),
                        devpath: x.devpath.clone(),
                        major: x.major.clone(),
                        minor: x.minor.clone(),
                        size: x.size.expect("Expected size"),
                        filesystem_type,
                        paths: x.paths.clone(),
                        children: hashset![],
                        mount_path,
                    }
                }).fold(children, |c, mut x| {
                    build_device_graph(&mut x, b, ys);

                    c.insert(x);

                    c
                });
        }
        Device::Mpath {
            devpath,
            children,
            major,
            minor,
            ..
        } => {
            let mut vs = get_vgs(&b, major, minor);

            let mut ps = get_partitions(&b, &ys, devpath);

            let zs = HashSet::unions(vec![vs, ps]);

            for mut z in zs {
                build_device_graph(&mut z, b, ys);

                children.insert(z);
            }
        }
        Device::ScsiDevice {
            devpath,
            children,
            paths,
            major,
            minor,
            ..
        }
        | Device::Partition {
            devpath,
            children,
            paths,
            major,
            minor,
            ..
        } => {
            let mut xs: HashSet<Device> = b
                .partitions
                .iter()
                .filter(|&x| match x.parent {
                    Some(ref p) => p == devpath,
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

                    Device::Partition {
                        partition_number: x.part_entry_number.expect("Expected part_entry_number"),
                        devpath: x.devpath.clone(),
                        major: x.major.clone(),
                        minor: x.minor.clone(),
                        size: x.size.expect("Expected size"),
                        paths: x.paths.clone(),
                        filesystem_type,
                        children: hashset![],
                        mount_path,
                    }
                }).collect();

            let mut ms: HashSet<Device> = b
                .mpaths
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

                    Device::Mpath {
                        serial: x.scsi83.clone().expect("Expected serial"),
                        size: x.size.expect("Expected size"),
                        major: x.major.clone(),
                        minor: x.minor.clone(),
                        paths: x.paths.clone(),
                        filesystem_type,
                        children: hashset![],
                        devpath: x.devpath.clone(),
                        mount_path,
                    }
                }).collect();

            let mut vs = get_vgs(b, major, minor);

            let mut mds: HashSet<Device> = b
                .mds
                .iter()
                .filter(|&x| intersections(vec![paths.clone(), x.md_devs.clone()]).is_empty())
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

                    Device::MdRaid {
                        paths: x.paths.clone(),
                        filesystem_type,
                        mount_path,
                        size: x.size.expect("Expected size"),
                        children: hashset![],
                        uuid: x.md_uuid.clone().expect("Expected md_uuid"),
                    }
                }).collect();

            let zs = HashSet::unions(vec![xs, ms, vs, mds]);

            for mut z in zs {
                build_device_graph(&mut z, b, ys);

                children.insert(z);
            }
        }
        Device::VolumeGroup { children, uuid, .. } => {
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

                    Device::LogicalVolume {
                        name: x.dm_lv_name.clone().expect("Expected dm_lv_name"),
                        devpath: x.devpath.clone(),
                        uuid: x.lv_uuid.clone().expect("Expected lv_uuid"),
                        size: x.size.expect("Expected size"),
                        paths: x.paths.clone(),
                        mount_path,
                        filesystem_type,
                        children: hashset![],
                    }
                }).fold(children, |c, mut x| {
                    build_device_graph(&mut x, b, ys);

                    c.insert(x);

                    c
                });
        }
        Device::LogicalVolume { .. } => {}
        Device::MdRaid { .. } => {}
        Device::Zpool { .. } => {}
        Device::Dataset { .. } => {}
    };
}

#[derive(Debug)]
struct Buckets<'a> {
    dms: Vector<&'a UEvent>,
    mds: Vector<&'a UEvent>,
    mpaths: Vector<&'a UEvent>,
    partitions: Vector<&'a UEvent>,
    rest: Vector<&'a UEvent>,
}

fn bucket_devices(xs: &Vector<UEvent>) -> Buckets {
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

fn add_parents<'a>(x: &'a mut UEvent, xs: &HashSet<PathBuf>) -> UEvent {
    let parent = x
        .devpath
        .parent()
        .expect("Expected parent path for devpath")
        .to_path_buf();

    let parent = if xs.contains(&parent) {
        Some(parent)
    } else {
        None
    };

    UEvent {
        parent,
        ..x.clone()
    }
}

fn build_device_list(x: &mut UEvents) -> Vector<UEvent> {
    let xs: HashSet<PathBuf> = x.clone().keys().cloned().collect();

    x.iter_mut()
        .filter(|y| keep_usable(y))
        .map(|x| add_parents(x, &xs))
        .collect()
}

type ConnectionTx = UnboundedSender<connections::Command<UnixStream>>;

pub fn handler() -> (
    UnboundedSender<(Command, ConnectionTx)>,
    impl Future<Item = State, Error = ()>,
) {
    let (tx, rx) = mpsc::unbounded();

    let fut = rx.fold(
        State::new(),
        move |State {
                  mut uevents,
                  mut local_mounts,
              }: State,
              (cmd, connections_tx): (
            Command,
            UnboundedSender<connections::Command<UnixStream>>,
        )| {
            {
                match cmd {
                    Command::UdevCommand(x) => update_udev(&mut uevents, x),
                    Command::MountCommand(x) => update_mount(&mut local_mounts, x),
                    _ => (),
                };
            };

            let dev_list = build_device_list(&mut uevents);
            let dev_list = bucket_devices(&dev_list);

            {
                let mut root = Device::Root {
                    children: hashset![],
                };

                build_device_graph(&mut root, &dev_list, &local_mounts);

                let s = serde_json::to_string::<Device>(&root)
                    .expect("Expected tree to serialize cleanly");

                connections_tx
                    .unbounded_send(connections::Command::Write(s))
                    .expect("Expected connection to send");
            };

            Ok(State {
                uevents,
                local_mounts,
            })
        },
    );

    (tx, fut)
}
