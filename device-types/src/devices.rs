// Copyright (c) 2019 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use crate::{mount, DevicePath};
use im::{hashset, HashSet, OrdSet};
use libzfs_types;
use std::path::PathBuf;

type Children = HashSet<Device>;
type Paths = OrdSet<DevicePath>;

#[derive(Debug, PartialEq, Eq, Hash, serde::Serialize, serde::Deserialize, Clone)]
pub struct Root {
    pub children: Children,
}

impl Default for Root {
    fn default() -> Self {
        Self {
            children: hashset![],
        }
    }
}

#[derive(Debug, PartialEq, Eq, Hash, serde::Serialize, serde::Deserialize, Clone)]
pub struct ScsiDevice {
    pub serial: String,
    pub scsi80: Option<String>,
    pub major: String,
    pub minor: String,
    pub devpath: PathBuf,
    pub size: u64,
    pub filesystem_type: Option<String>,
    pub paths: Paths,
    pub mount: Option<mount::Mount>,
    pub children: Children,
}

#[derive(Debug, PartialEq, Eq, Hash, serde::Serialize, serde::Deserialize, Clone)]
pub struct Partition {
    pub partition_number: u64,
    pub size: u64,
    pub major: String,
    pub minor: String,
    pub devpath: PathBuf,
    pub filesystem_type: Option<String>,
    pub paths: Paths,
    pub mount: Option<mount::Mount>,
    pub children: Children,
}

#[derive(Debug, PartialEq, Eq, Hash, serde::Serialize, serde::Deserialize, Clone)]
pub struct MdRaid {
    pub size: u64,
    pub major: String,
    pub minor: String,
    pub filesystem_type: Option<String>,
    pub paths: Paths,
    pub mount: Option<mount::Mount>,
    pub uuid: String,
    pub children: Children,
}

#[derive(Debug, PartialEq, Eq, Hash, serde::Serialize, serde::Deserialize, Clone)]
pub struct Mpath {
    pub devpath: PathBuf,
    pub serial: String,
    pub size: u64,
    pub major: String,
    pub minor: String,
    pub filesystem_type: Option<String>,
    pub paths: Paths,
    pub children: Children,
    pub mount: Option<mount::Mount>,
}

#[derive(Debug, PartialEq, Eq, Hash, serde::Serialize, serde::Deserialize, Clone)]
pub struct VolumeGroup {
    pub name: String,
    pub uuid: String,
    pub size: u64,
    pub children: Children,
}

#[derive(Debug, PartialEq, Eq, Hash, serde::Serialize, serde::Deserialize, Clone)]
pub struct LogicalVolume {
    pub name: String,
    pub uuid: String,
    pub major: String,
    pub minor: String,
    pub size: u64,
    pub children: Children,
    pub devpath: PathBuf,
    pub paths: Paths,
    pub filesystem_type: Option<String>,
    pub mount: Option<mount::Mount>,
}

#[derive(Debug, PartialEq, Eq, Hash, serde::Serialize, serde::Deserialize, Clone)]
pub struct Zpool {
    pub guid: u64,
    pub name: String,
    pub health: String,
    pub state: String,
    pub size: u64,
    pub vdev: libzfs_types::VDev,
    pub props: Vec<libzfs_types::ZProp>,
    pub children: Children,
}

#[derive(Debug, PartialEq, Eq, Hash, serde::Serialize, serde::Deserialize, Clone)]
pub struct Dataset {
    pub guid: String,
    pub name: String,
    pub kind: String,
    pub props: Vec<libzfs_types::ZProp>,
}

#[derive(Debug, PartialEq, Eq, Hash, serde::Serialize, serde::Deserialize, Clone)]
pub enum Device {
    Root(Root),
    ScsiDevice(ScsiDevice),
    Partition(Partition),
    MdRaid(MdRaid),
    Mpath(Mpath),
    VolumeGroup(VolumeGroup),
    LogicalVolume(LogicalVolume),
    Zpool(Zpool),
    Dataset(Dataset),
}
