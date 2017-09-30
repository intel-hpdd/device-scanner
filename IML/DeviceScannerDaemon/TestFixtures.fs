
module IML.DeviceScannerDaemon.TestFixtures

open Fable.Core.JsInterop
open Fable.PowerPack

open IML.DeviceScannerDaemon.EventTypes

let toJson =  Json.ofString >> Result.unwrapResult

let private object a =
  match a with
  | Json.Object a -> Some (Map.ofArray a)
  | _ -> None

let createEventJson (obj:Json.Json) (transformFn:Map<string, Json.Json> -> Map<string, Json.Json>) =
  obj
    |> object
    |> Option.get
    |> transformFn
    |> Map.toArray
    |> Json.Json.Object

let addObj =  toJson """
{
  "ACTION": "add",
  "DEVLINKS": "/dev/disk/by-id/ata-VBOX_HARDDISK_VB304a0a0f-15e93f07-part1 /dev/disk/by-path/pci-0000:00:01.1-ata-1.0-part1",
  "DEVNAME": "/dev/sdb1",
  "DEVPATH": "/devices/pci0000:00/0000:00:01.1/ata1/host1/target1:0:0/1:0:0:0/block/sdb/sdb1",
  "DEVTYPE": "partition",
  "ID_ATA": "1",
  "ID_ATA_FEATURE_SET_PM": 1,
  "ID_ATA_FEATURE_SET_PM_ENABLED": 1,
  "ID_ATA_WRITE_CACHE": 1,
  "ID_ATA_WRITE_CACHE_ENABLED": 1,
  "ID_BUS": "ata",
  "ID_MODEL": "VBOX_HARDDISK",
  "ID_MODEL_ENC": "VBOX HARDDISK",
  "ID_PART_ENTRY_DISK": "8:16",
  "ID_PART_ENTRY_NUMBER": "1",
  "ID_PART_ENTRY_OFFSET": "2048",
  "ID_PART_ENTRY_SCHEME": "dos",
  "ID_PART_ENTRY_SIZE": "2048",
  "ID_PART_ENTRY_TYPE": "0x83",
  "ID_PART_TABLE_TYPE": "dos",
  "ID_PATH": "pci-0000:00:01.1-ata-1.0",
  "ID_PATH_TAG": "pci-0000_00_01_1-ata-1_0",
  "ID_REVISION": "1.0",
  "ID_SERIAL": "VBOX_HARDDISK_VB304a0a0f-15e93f07",
  "ID_FS_TYPE": "LVM2_member",
  "ID_SERIAL_SHORT": "VB304a0a0f-15e93f07",
  "ID_TYPE": "disk",
  "MAJOR": "8",
  "MINOR": "17",
  "SEQNUM": "1566",
  "SUBSYSTEM": "block",
  "TAGS": ":systemd:",
  "USEC_INITIALIZED": "842",
  "IML_SIZE": "81784832",
  "IML_SCSI_80": "80",
  "IML_SCSI_83": "83",
  "IML_IS_RO": "1",
  "DM_MULTIPATH_DEVICE_PATH": "1",
  "DM_LV_NAME": "swap",
  "DM_VG_NAME": "centos"
}
"""

let removeObj = toJson """
{
  "ACTION": "remove",
  "DEVLINKS": "/dev/disk/by-id/ata-VBOX_HARDDISK_VB304a0a0f-15e93f07-part1 /dev/disk/by-path/pci-0000:00:01.1-ata-1.0-part1",
  "DEVNAME": "/dev/sdb1",
  "DEVPATH": "/devices/pci0000:00/0000:00:01.1/ata1/host1/target1:0:0/1:0:0:0/block/sdb/sdb1",
  "DEVTYPE": "partition",
  "ID_ATA": "1",
  "ID_ATA_FEATURE_SET_PM": 1,
  "ID_ATA_FEATURE_SET_PM_ENABLED": 1,
  "ID_ATA_WRITE_CACHE": 1,
  "ID_ATA_WRITE_CACHE_ENABLED": 1,
  "ID_BUS": "ata",
  "ID_MODEL": "VBOX_HARDDISK",
  "ID_MODEL_ENC": "VBOX HARDDISK",
  "ID_PART_ENTRY_DISK": "8:16",
  "ID_PART_ENTRY_NUMBER": "1",
  "ID_PART_ENTRY_OFFSET": "2048",
  "ID_PART_ENTRY_SCHEME": "dos",
  "ID_PART_ENTRY_SIZE": "2048",
  "ID_PART_ENTRY_TYPE": "0x83",
  "ID_PART_TABLE_TYPE": "dos",
  "ID_PATH": "pci-0000:00:01.1-ata-1.0",
  "ID_PATH_TAG": "pci-0000_00_01_1-ata-1_0",
  "ID_REVISION": "1.0",
  "ID_SERIAL": "VBOX_HARDDISK_VB304a0a0f-15e93f07",
  "ID_SERIAL_SHORT": "VB304a0a0f-15e93f07",
  "ID_TYPE": "disk",
  "MAJOR": "8",
  "MINOR": "17",
  "SEQNUM": "1566",
  "SUBSYSTEM": "block",
  "TAGS": ":systemd:",
  "USEC_INITIALIZED": "842"
}
"""
