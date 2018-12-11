// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#[macro_use]
extern crate serde_derive;

#[cfg(test)]
#[macro_use]
extern crate pretty_assertions;

pub mod devices;
pub mod uevent;

pub mod message {
    #[derive(Debug, Serialize, Deserialize)]
    pub enum Message {
        Data(String),
        Heartbeat,
    }
}

pub mod state {
    use crate::mount;
    use crate::uevent;
    use im::{HashMap, HashSet};
    use std::path::PathBuf;

    pub type UEvents = HashMap<PathBuf, uevent::UEvent>;

    pub type ZedEvents = HashMap<u64, libzfs_types::Pool>;

    #[derive(Debug, Clone, Default, Serialize, Deserialize)]
    pub struct State {
        pub uevents: UEvents,
        pub zed_events: ZedEvents,
        pub local_mounts: HashSet<mount::Mount>,
    }

    impl State {
        pub fn new() -> Self {
            State {
                uevents: HashMap::new(),
                zed_events: HashMap::new(),
                local_mounts: HashSet::new(),
            }
        }
    }
}

pub mod mount {
    use std::path::PathBuf;

    #[derive(Debug, Clone, PartialEq, Eq, Hash, Serialize, Deserialize)]
    pub struct MountPoint(pub PathBuf);

    #[derive(Debug, Clone, PartialEq, Eq, Hash, Serialize, Deserialize)]
    pub struct BdevPath(pub PathBuf);

    #[derive(Debug, Clone, PartialEq, Eq, Hash, Serialize, Deserialize)]
    pub struct FsType(pub String);

    #[derive(Debug, Clone, PartialEq, Eq, Hash, Serialize, Deserialize)]
    pub struct MountOpts(pub String);

    #[derive(Debug, PartialEq, Eq, Hash, Serialize, Deserialize, Clone)]
    pub struct Mount {
        pub target: MountPoint,
        pub source: BdevPath,
        pub fs_type: FsType,
        pub opts: MountOpts,
    }

    impl Mount {
        pub fn new(target: MountPoint, source: BdevPath, fs_type: FsType, opts: MountOpts) -> Self {
            Mount {
                target,
                source,
                fs_type,
                opts,
            }
        }
    }

    #[derive(Debug, PartialEq, Serialize, Deserialize)]
    pub enum MountCommand {
        AddMount(MountPoint, BdevPath, FsType, MountOpts),
        RemoveMount(MountPoint, BdevPath, FsType, MountOpts),
        ReplaceMount(MountPoint, BdevPath, FsType, MountOpts, MountOpts),
        MoveMount(MountPoint, BdevPath, FsType, MountOpts, MountPoint),
    }
}

pub mod udev {
    use crate::uevent;

    #[derive(Debug, PartialEq, Serialize, Deserialize)]
    pub enum UdevCommand {
        Add(uevent::UEvent),
        Change(uevent::UEvent),
        Remove(uevent::UEvent),
    }
}

pub mod zed {
    #[derive(Debug, PartialEq, Serialize, Deserialize)]
    pub enum PoolCommand {
        AddPools(Vec<libzfs_types::Pool>),
        AddPool(libzfs_types::Pool),
        UpdatePool(libzfs_types::Pool),
        RemovePool(zpool::Guid),
        AddDataset(zpool::Guid, libzfs_types::Dataset),
        RemoveDataset(zpool::Guid, zfs::Name),
        SetZpoolProp(zpool::Guid, prop::Key, prop::Value),
        SetZfsProp(zpool::Guid, zfs::Name, prop::Key, prop::Value),
    }

    pub mod zpool {
        #[derive(Debug, PartialEq, Serialize, Deserialize)]
        pub struct Name(pub String);

        #[derive(Debug, PartialEq, Serialize, Deserialize)]
        pub struct Guid(pub String);

        impl From<u64> for Guid {
            fn from(x: u64) -> Self {
                Guid(format!("{:#018X}", x))
            }
        }

        impl From<Guid> for Result<u64, std::num::ParseIntError> {
            fn from(Guid(x): Guid) -> Self {
                let without_prefix = x.trim_left_matches("0x");
                u64::from_str_radix(without_prefix, 16)
            }
        }

        #[derive(Debug, PartialEq, Serialize, Deserialize)]
        pub struct State(pub String);

        impl From<State> for String {
            fn from(State(x): State) -> Self {
                x
            }
        }
    }

    pub mod zfs {
        #[derive(Debug, PartialEq, Serialize, Deserialize)]
        pub struct Name(pub String);
    }

    pub mod prop {
        #[derive(Debug, PartialEq, Serialize, Deserialize)]
        pub struct Key(pub String);

        #[derive(Debug, PartialEq, Serialize, Deserialize)]
        pub struct Value(pub String);
    }

    #[derive(Debug, PartialEq, Serialize, Deserialize)]
    pub enum ZedCommand {
        Init,
        CreateZpool(zpool::Name, zpool::Guid, zpool::State),
        ImportZpool(zpool::Name, zpool::Guid, zpool::State),
        ExportZpool(zpool::Guid, zpool::State),
        DestroyZpool(zpool::Guid),
        CreateZfs(zpool::Guid, zfs::Name),
        DestroyZfs(zpool::Guid, zfs::Name),
        SetZpoolProp(zpool::Guid, prop::Key, prop::Value),
        SetZfsProp(zpool::Guid, zfs::Name, prop::Key, prop::Value),
        AddVdev(zpool::Name, zpool::Guid),
    }
}

#[derive(Debug, PartialEq, Serialize, Deserialize)]
pub enum Command {
    Stream,
    PoolCommand(zed::PoolCommand),
    UdevCommand(udev::UdevCommand),
    MountCommand(mount::MountCommand),
}

#[cfg(test)]
mod tests {
    extern crate serde_json;

    use super::{mount, *};
    use std::path::PathBuf;

    #[test]
    fn test_mount_deserialize() {
        let s = "{\"MountCommand\":{\"AddMount\":[\"swap\",\"/dev/mapper/VolGroup00-LogVol01\",\"swap\",\"defaults\"]}}";

        let result = serde_json::from_str::<Command>(s).unwrap();

        assert_eq!(
            result,
            Command::MountCommand(mount::MountCommand::AddMount(
                mount::MountPoint({
                    let mut p = PathBuf::new();
                    p.push("swap");

                    p
                }),
                mount::BdevPath({
                    let mut p = PathBuf::new();
                    p.push("/dev/mapper/VolGroup00-LogVol01".to_string());

                    p
                }),
                mount::FsType("swap".to_string()),
                mount::MountOpts("defaults".to_string()),
            ))
        )
    }
}
