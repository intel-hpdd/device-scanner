// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec UdevEventTypes.EventTypes

open Fable.Core

[<StringEnum>]
type Add = Add

[<StringEnum>]
type Remove = Remove

[<StringEnum>]
type Actions =
  | Add
  | Remove

type IAction =
  abstract ACTION: Actions with get, set

[<StringEnum>]
type IDevType =
  | Partition
  | Disk

type IAdd =
  abstract ACTION: Add with get
  abstract MAJOR: string with get
  abstract MINOR: string with get
  abstract DEVLINKS: string with get
  abstract DEVNAME: string with get
  abstract DEVPATH: string with get
  abstract DEVTYPE: IDevType with get
  abstract ID_VENDOR: string option with get
  abstract ID_MODEL: string with get
  abstract ID_SERIAL: string option with get
  abstract ID_FS_TYPE: string with get
  abstract ID_PART_ENTRY_NUMBER: string option with get
  abstract IML_SIZE: string

type IRemove =
  abstract ACTION: Remove with get
  abstract DEVLINKS: string with get
  abstract DEVPATH: string with get
  abstract MAJOR: string with get
  abstract MINOR: string with get

type Events =
  | Add of IAdd
  | Remove of IRemove
  | Info


// UDEV  [890.275494] remove   /devices/pci0000:00/0000:00:01.1/ata1/host1/target1:0:0/1:0:0:0/block/sdb/sdb1 (block)
// ACTION=remove
// DEVLINKS=/dev/disk/by-id/ata-VBOX_HARDDISK_VB304a0a0f-15e93f07-part1 /dev/disk/by-path/pci-0000:00:01.1-ata-1.0-part1
// DEVNAME=/dev/sdb1
// DEVPATH=/devices/pci0000:00/0000:00:01.1/ata1/host1/target1:0:0/1:0:0:0/block/sdb/sdb1
// DEVTYPE=partition
// ID_ATA=1
// ID_ATA_FEATURE_SET_PM=1
// ID_ATA_FEATURE_SET_PM_ENABLED=1
// ID_ATA_WRITE_CACHE=1
// ID_ATA_WRITE_CACHE_ENABLED=1
// ID_BUS=ata
// ID_MODEL=VBOX_HARDDISK
// ID_MODEL_ENC=VBOX\x20HARDDISK\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20\x20
// ID_PART_ENTRY_DISK=8:16
// ID_PART_ENTRY_NUMBER=1
// ID_PART_ENTRY_OFFSET=2048
// ID_PART_ENTRY_SCHEME=dos
// ID_PART_ENTRY_SIZE=2048
// ID_PART_ENTRY_TYPE=0x83
// ID_PART_TABLE_TYPE=dos
// ID_PATH=pci-0000:00:01.1-ata-1.0
// ID_PATH_TAG=pci-0000_00_01_1-ata-1_0
// ID_REVISION=1.0
// ID_SERIAL=VBOX_HARDDISK_VB304a0a0f-15e93f07
// ID_SERIAL_SHORT=VB304a0a0f-15e93f07
// ID_TYPE=disk
// MAJOR=8
// MINOR=17
// SEQNUM=1566
// SUBSYSTEM=block
// TAGS=:systemd:
// USEC_INITIALIZED=842
