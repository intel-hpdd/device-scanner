use im::{HashSet, OrdSet};
use libzfs_types;
use std::path::PathBuf;

pub type Paths = OrdSet<PathBuf>;

pub type MountPath = Option<PathBuf>;

#[derive(Debug, Eq, PartialEq, Hash, Serialize, Deserialize, Clone, Default)]
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

impl std::fmt::Display for DeviceType {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        match self {
            DeviceType::Host => write!(f, "host"),
            DeviceType::ScsiDevice => write!(f, "scsi device"),
            DeviceType::Partition => write!(f, "partition"),
            DeviceType::MdRaid => write!(f, "mdraid"),
            DeviceType::Mpath => write!(f, "multipath"),
            DeviceType::VolumeGroup => write!(f, "volume group"),
            DeviceType::LogicalVolume => write!(f, "logical volume"),
            DeviceType::Zpool => write!(f, "zpool"),
            DeviceType::Dataset => write!(f, "dataset"),
        }
    }
}

pub trait Type {
    fn name(&self) -> DeviceType;
}

pub trait AsParent {
    fn as_parent(&self) -> Parent;
}

pub trait MountableStorageDevice: Type {
    fn paths(&self) -> &Paths;
    fn serial(&self) -> &Serial;
    fn mount_path(&self) -> &MountPath;
    fn size(&self) -> i64;
    fn filesystem_type(&self) -> &Option<String>;
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone, Default)]
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

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone, Default)]
pub struct ScsiDevice {
    pub serial: Serial,
    pub major: String,
    pub minor: String,
    pub devpath: PathBuf,
    pub size: i64,
    pub filesystem_type: Option<String>,
    pub filesystem_label: Option<String>,
    pub paths: Paths,
    pub mount_path: MountPath,
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

impl MountableStorageDevice for ScsiDevice {
    fn paths(&self) -> &Paths {
        &self.paths
    }
    fn serial(&self) -> &Serial {
        &self.serial
    }
    fn mount_path(&self) -> &MountPath {
        &self.mount_path
    }
    fn size(&self) -> i64 {
        self.size
    }
    fn filesystem_type(&self) -> &Option<String> {
        &self.filesystem_type
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone, Default)]
pub struct Partition {
    pub serial: Serial,
    pub partition_number: i64,
    pub parents: Parents,
    pub size: i64,
    pub major: String,
    pub minor: String,
    pub devpath: PathBuf,
    pub filesystem_type: Option<String>,
    pub filesystem_label: Option<String>,
    pub paths: Paths,
    pub mount_path: MountPath,
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

impl MountableStorageDevice for Partition {
    fn paths(&self) -> &Paths {
        &self.paths
    }
    fn serial(&self) -> &Serial {
        &self.serial
    }
    fn mount_path(&self) -> &MountPath {
        &self.mount_path
    }
    fn size(&self) -> i64 {
        self.size
    }
    fn filesystem_type(&self) -> &Option<String> {
        &self.filesystem_type
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone, Default)]
pub struct MdRaid {
    pub serial: Serial,
    pub size: i64,
    pub major: String,
    pub minor: String,
    pub filesystem_type: Option<String>,
    pub filesystem_label: Option<String>,
    pub paths: Paths,
    pub parents: Parents,
    pub mount_path: MountPath,
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

impl MountableStorageDevice for MdRaid {
    fn paths(&self) -> &Paths {
        &self.paths
    }
    fn serial(&self) -> &Serial {
        &self.serial
    }
    fn mount_path(&self) -> &MountPath {
        &self.mount_path
    }
    fn size(&self) -> i64 {
        self.size
    }
    fn filesystem_type(&self) -> &Option<String> {
        &self.filesystem_type
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone, Default)]
pub struct Mpath {
    pub devpath: PathBuf,
    pub serial: Serial,
    pub size: i64,
    pub major: String,
    pub minor: String,
    pub parents: Parents,
    pub filesystem_type: Option<String>,
    pub filesystem_label: Option<String>,
    pub paths: Paths,
    pub mount_path: MountPath,
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

impl MountableStorageDevice for Mpath {
    fn paths(&self) -> &Paths {
        &self.paths
    }
    fn serial(&self) -> &Serial {
        &self.serial
    }
    fn mount_path(&self) -> &MountPath {
        &self.mount_path
    }
    fn size(&self) -> i64 {
        self.size
    }
    fn filesystem_type(&self) -> &Option<String> {
        &self.filesystem_type
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct VolumeGroup {
    pub serial: Serial,
    pub name: String,
    pub size: i64,
    pub parents: Parents,
}

impl Default for VolumeGroup {
    fn default() -> Self {
        VolumeGroup {
            serial: Default::default(),
            name: Default::default(),
            size: Default::default(),
            parents: im::hashset![],
        }
    }
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
    pub filesystem_label: Option<String>,
    pub mount_path: MountPath,
}

impl Default for LogicalVolume {
    fn default() -> Self {
        LogicalVolume {
            serial: Default::default(),
            name: Default::default(),
            parent: (DeviceType::VolumeGroup, Serial(Default::default())),
            major: Default::default(),
            minor: Default::default(),
            size: Default::default(),
            devpath: Default::default(),
            paths: Default::default(),
            filesystem_type: Default::default(),
            filesystem_label: Default::default(),
            mount_path: Default::default(),
        }
    }
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

impl MountableStorageDevice for LogicalVolume {
    fn paths(&self) -> &Paths {
        &self.paths
    }
    fn serial(&self) -> &Serial {
        &self.serial
    }
    fn mount_path(&self) -> &MountPath {
        &self.mount_path
    }
    fn size(&self) -> i64 {
        self.size
    }
    fn filesystem_type(&self) -> &Option<String> {
        &self.filesystem_type
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone)]
pub struct Zpool {
    pub serial: Serial,
    pub name: String,
    pub health: String,
    pub state: String,
    pub size: i64,
    pub parents: Parents,
    pub props: Vec<libzfs_types::ZProp>,
    pub paths: Paths,
    pub filesystem_type: Option<String>,
    pub filesystem_label: Option<String>,
    pub mount_path: MountPath,
}

impl Type for Zpool {
    fn name(&self) -> DeviceType {
        DeviceType::Zpool
    }
}

impl std::fmt::Display for Zpool {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "Zpool: {} {}", self.name, self.serial)
    }
}

impl AsParent for Zpool {
    fn as_parent(&self) -> Parent {
        (self.name(), self.serial.clone())
    }
}

impl MountableStorageDevice for Zpool {
    fn paths(&self) -> &Paths {
        &self.paths
    }
    fn serial(&self) -> &Serial {
        &self.serial
    }
    fn mount_path(&self) -> &MountPath {
        &self.mount_path
    }
    fn size(&self) -> i64 {
        self.size
    }
    fn filesystem_type(&self) -> &Option<String> {
        &self.filesystem_type
    }
}

#[derive(Debug, PartialEq, Eq, Serialize, Hash, Deserialize, Clone, Default)]
pub struct Dataset {
    pub serial: Serial,
    pub pool_serial: Serial,
    pub size: i64,
    pub name: String,
    pub kind: String,
    pub props: Vec<libzfs_types::ZProp>,
    pub paths: Paths,
    pub mount_path: MountPath,
    pub filesystem_type: Option<String>,
    pub filesystem_label: Option<String>,
}

impl Type for Dataset {
    fn name(&self) -> DeviceType {
        DeviceType::Dataset
    }
}

impl std::fmt::Display for Dataset {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "Dataset: {} {}", self.name, self.serial)
    }
}

impl AsParent for Dataset {
    fn as_parent(&self) -> Parent {
        (self.name(), self.serial.clone())
    }
}

impl MountableStorageDevice for Dataset {
    fn paths(&self) -> &Paths {
        &self.paths
    }
    fn serial(&self) -> &Serial {
        &self.serial
    }
    fn mount_path(&self) -> &MountPath {
        &self.mount_path
    }
    fn size(&self) -> i64 {
        self.size
    }
    fn filesystem_type(&self) -> &Option<String> {
        &self.filesystem_type
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

impl Device {
    pub fn as_mountable_storage_device(&self) -> Option<&MountableStorageDevice> {
        match self {
            Device::Host(_) => None,
            Device::Dataset(x) => Some(x as &MountableStorageDevice),
            Device::Zpool(x) => Some(x as &MountableStorageDevice),
            Device::LogicalVolume(x) => Some(x as &MountableStorageDevice),
            Device::MdRaid(x) => Some(x as &MountableStorageDevice),
            Device::Mpath(x) => Some(x as &MountableStorageDevice),
            Device::Partition(x) => Some(x as &MountableStorageDevice),
            Device::ScsiDevice(x) => Some(x as &MountableStorageDevice),
            Device::VolumeGroup(_) => None,
        }
    }
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

impl Type for Device {
    fn name(&self) -> DeviceType {
        match self {
            Device::Host(x) => x.name(),
            Device::Dataset(x) => x.name(),
            Device::Zpool(x) => x.name(),
            Device::LogicalVolume(x) => x.name(),
            Device::MdRaid(x) => x.name(),
            Device::Mpath(x) => x.name(),
            Device::Partition(x) => x.name(),
            Device::ScsiDevice(x) => x.name(),
            Device::VolumeGroup(x) => x.name(),
        }
    }
}

impl Device {
    pub fn is_scsi(&self) -> bool {
        self.name() == DeviceType::ScsiDevice
    }
    pub fn is_host(&self) -> bool {
        self.name() == DeviceType::Host
    }
}
