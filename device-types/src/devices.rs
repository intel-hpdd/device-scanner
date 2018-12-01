use im::HashSet;
use libzfs_types;
use std::path::PathBuf;

pub type Paths = HashSet<PathBuf>;

#[derive(Debug, Eq, PartialEq, Hash, Serialize, Deserialize, Clone)]
pub struct Serial(pub String);

impl std::fmt::Display for Serial {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "{}", self.0)
    }
}

impl From<String> for Serial {
    fn from(s: String) -> Self {
        Serial(s)
    }
}

/// A pointer to a parent device.
/// This is basically a unique composite key
pub type Parent = (DeviceType, Serial);

pub type Parents = HashSet<Parent>;

#[derive(Debug, PartialEq, Eq, Hash, Serialize, Deserialize, Clone)]
pub enum DeviceType {
    Host,
    ScsiDevice,
    Partition,
    MdRaid,
    Mpath,
    VolumeGroup,
    LogicalVolume,
    Zpool,
    Dataset,
}

pub trait Type {
    fn name(&self) -> DeviceType;
}

pub trait AsParent {
    fn as_parent(&self) -> Parent;
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct Host(pub String);

impl Type for Host {
    fn name(&self) -> DeviceType {
        DeviceType::Host
    }
}

impl AsParent for Host {
    fn as_parent(&self) -> Parent {
        (self.name(), Serial(self.0.clone()))
    }
}

impl std::fmt::Display for Host {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "Host: {}", self.0)
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct ScsiDevice {
    pub serial: Serial,
    pub major: String,
    pub minor: String,
    pub devpath: PathBuf,
    pub size: i64,
    pub filesystem_type: Option<String>,
    pub paths: Paths,
    pub mount_path: Option<PathBuf>,
}

impl Type for ScsiDevice {
    fn name(&self) -> DeviceType {
        DeviceType::ScsiDevice
    }
}

impl AsParent for ScsiDevice {
    fn as_parent(&self) -> Parent {
        (self.name(), self.serial.clone())
    }
}

impl std::fmt::Display for ScsiDevice {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "Scsi: {}", self.serial)
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct Partition {
    pub serial: Serial,
    pub partition_number: i64,
    pub parents: Parents,
    pub size: i64,
    pub major: String,
    pub minor: String,
    pub devpath: PathBuf,
    pub filesystem_type: Option<String>,
    pub paths: Paths,
    pub mount_path: Option<PathBuf>,
}

impl Type for Partition {
    fn name(&self) -> DeviceType {
        DeviceType::Partition
    }
}

impl AsParent for Partition {
    fn as_parent(&self) -> Parent {
        (self.name(), self.serial.clone())
    }
}

impl std::fmt::Display for Partition {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "Partition: {}", self.serial)
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct MdRaid {
    pub serial: Serial,
    pub size: i64,
    pub major: String,
    pub minor: String,
    pub filesystem_type: Option<String>,
    pub paths: Paths,
    pub parents: Parents,
    pub mount_path: Option<PathBuf>,
    pub uuid: String,
}

impl Type for MdRaid {
    fn name(&self) -> DeviceType {
        DeviceType::MdRaid
    }
}

impl AsParent for MdRaid {
    fn as_parent(&self) -> Parent {
        (self.name(), self.serial.clone())
    }
}

impl std::fmt::Display for MdRaid {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "MdRaid: {}", self.serial)
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct Mpath {
    pub devpath: PathBuf,
    pub serial: Serial,
    pub size: i64,
    pub major: String,
    pub minor: String,
    pub parents: Parents,
    pub filesystem_type: Option<String>,
    pub paths: Paths,
    pub mount_path: Option<PathBuf>,
}

impl Type for Mpath {
    fn name(&self) -> DeviceType {
        DeviceType::Mpath
    }
}

impl AsParent for Mpath {
    fn as_parent(&self) -> Parent {
        (self.name(), self.serial.clone())
    }
}

impl std::fmt::Display for Mpath {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "Mpath: {}", self.serial)
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct VolumeGroup {
    pub serial: Serial,
    pub name: String,
    pub size: i64,
    pub parents: Parents,
}

impl Type for VolumeGroup {
    fn name(&self) -> DeviceType {
        DeviceType::VolumeGroup
    }
}

impl AsParent for VolumeGroup {
    fn as_parent(&self) -> Parent {
        (self.name(), self.serial.clone())
    }
}

impl std::fmt::Display for VolumeGroup {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "VG: {} {}", self.name, self.serial)
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct LogicalVolume {
    pub serial: Serial,
    pub name: String,
    pub parent: Parent,
    pub major: String,
    pub minor: String,
    pub size: i64,
    pub devpath: PathBuf,
    pub paths: Paths,
    pub filesystem_type: Option<String>,
    pub mount_path: Option<PathBuf>,
}

impl Type for LogicalVolume {
    fn name(&self) -> DeviceType {
        DeviceType::LogicalVolume
    }
}

impl AsParent for LogicalVolume {
    fn as_parent(&self) -> Parent {
        (self.name(), self.serial.clone())
    }
}

impl std::fmt::Display for LogicalVolume {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "LV: {} {}", self.name, self.serial)
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct Zpool {
    pub guid: u64,
    pub name: String,
    pub health: String,
    pub state: String,
    pub size: u64,
    pub vdev: libzfs_types::VDev,
    pub props: Vec<libzfs_types::ZProp>,
}

impl Type for Zpool {
    fn name(&self) -> DeviceType {
        DeviceType::Zpool
    }
}

impl std::fmt::Display for Zpool {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "Zpool: {} {}", self.name, self.guid)
    }
}

impl AsParent for Zpool {
    fn as_parent(&self) -> Parent {
        (self.name(), Serial(self.guid.to_string()))
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct Dataset {
    pub guid: String,
    pub pool_guid: u64,
    pub name: String,
    pub kind: String,
    pub props: Vec<libzfs_types::ZProp>,
}

impl Type for Dataset {
    fn name(&self) -> DeviceType {
        DeviceType::Dataset
    }
}

impl std::fmt::Display for Dataset {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "Dataset: {} {}", self.name, self.guid)
    }
}

impl AsParent for Dataset {
    fn as_parent(&self) -> Parent {
        (self.name(), Serial(self.guid.to_string()))
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub enum Device {
    Host(Host),
    ScsiDevice(ScsiDevice),
    Partition(Partition),
    MdRaid(MdRaid),
    Mpath(Mpath),
    VolumeGroup(VolumeGroup),
    LogicalVolume(LogicalVolume),
    Zpool(Zpool),
    Dataset(Dataset),
}

impl std::fmt::Display for Device {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        match self {
            Device::Host(x) => write!(f, "{}", x),
            Device::Dataset(x) => write!(f, "{}", x),
            Device::Zpool(x) => write!(f, "{}", x),
            Device::LogicalVolume(x) => write!(f, "{}", x),
            Device::MdRaid(x) => write!(f, "{}", x),
            Device::Mpath(x) => write!(f, "{}", x),
            Device::Partition(x) => write!(f, "{}", x),
            Device::ScsiDevice(x) => write!(f, "{}", x),
            Device::VolumeGroup(x) => write!(f, "{}", x),
        }
    }
}

impl AsParent for Device {
    fn as_parent(&self) -> Parent {
        match self {
            Device::Host(x) => x.as_parent(),
            Device::Dataset(x) => x.as_parent(),
            Device::Zpool(x) => x.as_parent(),
            Device::LogicalVolume(x) => x.as_parent(),
            Device::MdRaid(x) => x.as_parent(),
            Device::Mpath(x) => x.as_parent(),
            Device::Partition(x) => x.as_parent(),
            Device::ScsiDevice(x) => x.as_parent(),
            Device::VolumeGroup(x) => x.as_parent(),
        }
    }
}
