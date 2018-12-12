use im::{OrdSet, Vector};
use std::path::PathBuf;

use crate::devices;

#[derive(Debug, PartialEq, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct UEvent {
    pub major: String,
    pub minor: String,
    pub seqnum: i64,
    pub paths: OrdSet<PathBuf>,
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
    pub md_devs: OrdSet<PathBuf>,
    pub dm_multipath_devpath: Option<bool>,
    pub dm_name: Option<String>,
    pub dm_lv_name: Option<String>,
    pub lv_uuid: Option<String>,
    pub dm_vg_name: Option<String>,
    pub vg_uuid: Option<String>,
    pub md_uuid: Option<String>,
}

impl UEvent {
    pub fn get_type(&self) -> devices::DeviceType {
        if self.is_dm() {
            devices::DeviceType::VolumeGroup
        } else if self.is_mdraid() {
            devices::DeviceType::MdRaid
        } else if self.is_mpath() {
            devices::DeviceType::Mpath
        } else if self.is_partition() {
            devices::DeviceType::Partition
        } else {
            devices::DeviceType::ScsiDevice
        }
    }

    pub fn is_mpath(&self) -> bool {
        self.is_mpath == Some(true)
    }

    pub fn is_dm(&self) -> bool {
        [&self.lv_uuid, &self.vg_uuid, &self.dm_lv_name]
            .iter()
            .all(|x| x.is_some())
    }

    pub fn is_partition(&self) -> bool {
        self.part_entry_mm.is_some()
    }

    pub fn is_mdraid(&self) -> bool {
        self.md_uuid.is_some()
    }

    /// Filter out any devices that are not suitable for mounting a filesystem.
    pub fn keep_usable(&self) -> bool {
        self.size.filter(|x| *x > 0).is_some()
            && self.read_only != Some(true)
            && self.bios_boot != Some(true)
    }

    pub fn get_serial(&self) -> std::io::Result<devices::Serial> {
        self.scsi83.clone().map(devices::Serial).ok_or_else(|| {
            std::io::Error::new(
                std::io::ErrorKind::NotFound,
                format!("Expected serial while parsing: {:?}", self),
            )
        })
    }
}
