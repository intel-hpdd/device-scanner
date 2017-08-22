module IML.UdevEventTypes.EventTypesTest

open IML.UdevEventTypes.EventTypes
open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open Fable.Core.JsInterop
open Fable.PowerPack

let toJson =  Json.ofString >> Result.unwrapResult

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

let matcher = function
  | InfoEventMatch x -> "infoEvent"
  | AddEventMatch x -> "addEvent"
  | RemoveEventMatch x -> "removeEvent"
  | _ -> "no match"

test "Matching Events" <| fun () ->
  expect.assertions 4

  matcher addObj === "addEvent"

  matcher removeObj === "removeEvent"

  matcher (toJson """{ "ACTION": "info" }""") === "infoEvent"

  matcher (toJson """{ "ACTION": "blah" }""") === "no match"
