module IML.Common

open Fable.PowerPack
open Json
open Fable.Core

let hasPair k v m =
  m
    |> Map.tryFindKey (fun k' v' -> k = k' && v = v')
    |> Option.isSome

let hasAction v =
  hasPair "ACTION" (String v)

[<Erase>]
type DevPath = DevPath of string
[<Erase>]
type Path = Path of string

/// Describes a block device
type AddEvent = {
  MAJOR: string;
  MINOR: string;
  PATHS: Path array option;
  DEVNAME: Path;
  DEVPATH: DevPath;
  DEVTYPE: string;
  ID_VENDOR: string option;
  ID_MODEL: string option;
  ID_SERIAL: string option;
  ID_FS_TYPE: string option;
  ID_FS_USAGE: string option;
  ID_PART_ENTRY_NUMBER: int option;
  IML_SIZE: string option;
  IML_SCSI_80: string option;
  IML_SCSI_83: string option;
  IML_IS_RO: bool option;
  IML_DM_SLAVE_MMS: string [];
  IML_DM_VG_SIZE: string option;
  IML_MD_DEVICES: string [];
  DM_MULTIPATH_DEVICE_PATH: bool option;
  DM_LV_NAME: string option;
  DM_VG_NAME: string option;
  DM_UUID: string option;
  MD_UUID: string option;
}

[<Erase>]
type ZfsPoolUid = ZfsPoolUid of string
[<Erase>]
type ZfsDatasetUid = ZfsDatasetUid of string

type ZfsDataset = {
  POOL_UID: ZfsPoolUid;
  DATASET_NAME: string;
  DATASET_UID: ZfsDatasetUid;
  PROPERTIES: Map<string, string>;
}

// type ZfsProperty = {
  // POOL_UID: ZfsPoolUid;
  // DATASET_UID: ZfsDatasetUid option;
  // PROPERTY_NAME: string;
  // PROPERTY_VALUE: string;
// }

type ZfsPool = {
  NAME: string;
  UID: ZfsPoolUid;
  STATE_STR: string;
  PATH: string;
  DATASETS: Map<ZfsDatasetUid, ZfsDataset>;
  PROPERTIES: Map<string, string>;
}

type BlockDevMap = Map<DevPath, AddEvent>
type ZfsMap = Map<ZfsPoolUid, ZfsPool>

type DevMaps = {
  BLOCK_DEVICES: BlockDevMap;
  ZFSPOOLS: ZfsMap;
}
