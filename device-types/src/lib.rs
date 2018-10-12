#[macro_use]
extern crate serde_derive;

#[cfg(test)]
#[macro_use]
extern crate pretty_assertions;

extern crate im;

pub mod state {
    use im::{HashMap, HashSet};
    use mount;
    use std::path::PathBuf;
    use uevent;

    pub type UEvents = HashMap<PathBuf, uevent::UEvent>;

    #[derive(Default, Serialize)]
    pub struct State {
        pub uevents: UEvents,
        pub local_mounts: HashSet<mount::Mount>,
    }

    impl State {
        pub fn new() -> Self {
            State {
                uevents: HashMap::new(),
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
    use uevent;

    #[derive(Debug, PartialEq, Serialize, Deserialize)]
    pub enum UdevCommand {
        Add(uevent::UEvent),
        Change(uevent::UEvent),
        Remove(uevent::UEvent),
    }
}

pub mod uevent {
    use im::{HashSet, Vector};
    use std::path::PathBuf;

    #[derive(Debug, PartialEq, Serialize, Deserialize, Clone)]
    #[serde(rename_all = "camelCase")]
    pub struct UEvent {
        pub major: String,
        pub minor: String,
        pub seqnum: i64,
        pub paths: HashSet<PathBuf>,
        pub devname: PathBuf,
        pub devpath: PathBuf,
        pub devtype: String,
        pub vendor: Option<String>,
        pub model: Option<String>,
        pub serial: Option<String>,
        pub fs_type: Option<String>,
        pub fs_usage: Option<String>,
        pub fs_uuid: Option<String>,
        pub part_entry_number: Option<i64>,
        pub part_entry_mm: Option<String>,
        pub size: Option<i64>,
        pub scsi80: Option<String>,
        pub scsi83: Option<String>,
        pub read_only: Option<bool>,
        pub bios_boot: Option<bool>,
        pub zfs_reserved: Option<bool>,
        pub is_mpath: Option<bool>,
        pub dm_slave_mms: Vector<String>,
        pub dm_vg_size: Option<i64>,
        pub md_devs: HashSet<PathBuf>,
        pub dm_multipath_devpath: Option<bool>,
        pub dm_name: Option<String>,
        pub dm_lv_name: Option<String>,
        pub lv_uuid: Option<String>,
        pub dm_vg_name: Option<String>,
        pub vg_uuid: Option<String>,
        pub md_uuid: Option<String>,
    }
}

pub mod zed {
    #[derive(Debug, PartialEq, Serialize, Deserialize)]
    pub enum ZedCommand {}
}

#[derive(Debug, PartialEq, Serialize, Deserialize)]
pub enum Command {
    Stream,
    ZedCommand(zed::ZedCommand),
    UdevCommand(udev::UdevCommand),
    MountCommand(mount::MountCommand),
}

pub mod devices {
    use im::HashSet;
    use std::path::PathBuf;

    type Children = HashSet<Device>;
    type Paths = HashSet<PathBuf>;

    #[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
    pub enum Device {
        Root {
            children: Children,
        },
        ScsiDevice {
            serial: String,
            major: String,
            minor: String,
            devpath: PathBuf,
            size: i64,
            filesystem_type: Option<String>,
            paths: Paths,
            mount_path: Option<PathBuf>,
            children: Children,
        },
        Partition {
            partition_number: i64,
            size: i64,
            major: String,
            minor: String,
            devpath: PathBuf,
            filesystem_type: Option<String>,
            paths: Paths,
            mount_path: Option<PathBuf>,
            children: Children,
        },
        MdRaid {
            size: i64,
            filesystem_type: Option<String>,
            paths: Paths,
            mount_path: Option<PathBuf>,
            uuid: String,
            children: Children,
        },
        Mpath {
            devpath: PathBuf,
            serial: String,
            size: i64,
            major: String,
            minor: String,
            filesystem_type: Option<String>,
            paths: Paths,
            children: Children,
            mount_path: Option<PathBuf>,
        },
        VolumeGroup {
            name: String,
            uuid: String,
            size: i64,
            children: Children,
        },
        LogicalVolume {
            name: String,
            uuid: String,
            major: String,
            minor: String,
            size: i64,
            children: Children,
            devpath: PathBuf,
            paths: Paths,
            filesystem_type: Option<String>,
            mount_path: Option<PathBuf>,
        },
        Zpool {
            guid: String,
            name: String,
            children: Children,
            size: i64,
        },
        Dataset {
            guid: String,
            name: String,
        },
    }
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
                mount::MountOpts("defaults".to_string())
            ))
        )
    }
}
