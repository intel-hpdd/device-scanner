// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.EventTypes

open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.Core

[<StringEnum>]
type DevType =
  | Partition
  | Disk

let (|PartitionMatch|_|) (x:string) =
  if x = "partition" then
    Some Partition
  else
    None

let (|DiskMatch|_|) (x:string) =
  if x = "disk" then
    Some Disk
  else
    None

[<Erase>]
type Action = Action of string
[<Erase>]
type Major = Major of string
[<Erase>]
type Minor = Minor of string
[<Erase>]
type DevPath = DevPath of string
[<Erase>]
type DevLinks = DevLinks of string
[<Erase>]
type Path = Path of string
[<Erase>]
type IdVendor = IdVendor of string
[<Erase>]
type IdModel = IdModel of string
[<Erase>]
type IdSerial = IdSerial of string
[<Erase>]
type IdFsType = IdFsType of string
[<Erase>]
type IdPartEntryNumber = IdPartEntryNumber of int
[<Erase>]
type ImlSize = ImlSize of string
[<Erase>]
type ImlScsi80 = ImlScsi80 of string
[<Erase>]
type ImlScsi83 = ImlScsi83 of string
[<Erase>]
type ImlIsRo = ImlIsRo of bool

[<Erase>]
type DmMultipathDevicePath = DmMultipathDevicePath of bool

[<Erase>]
type DmLvName = DmLvName of string

[<Erase>]
type DmVgName = DmVgName of string

let addAction = Action("add")
let changeAction = Action("change")
let removeAction = Action("remove")
let infoAction = Action("info")


/// The data received from a
/// udev block device add event
type AddEvent = {
  ACTION: Action;
  MAJOR: Major;
  MINOR: Minor;
  DEVLINKS: DevLinks option;
  PATHS: Path array option;
  DEVNAME: Path;
  DEVPATH: DevPath;
  DEVTYPE: DevType;
  ID_VENDOR: IdVendor option;
  ID_MODEL: IdModel option;
  ID_SERIAL: IdSerial option;
  ID_FS_TYPE: IdFsType option;
  ID_PART_ENTRY_NUMBER: IdPartEntryNumber option;
  IML_SIZE: ImlSize option;
  IML_SCSI_80: ImlScsi80 option;
  IML_SCSI_83: ImlScsi83 option;
  IML_IS_RO: ImlIsRo option;
  DM_MULTIPATH_DEVICE_PATH: DmMultipathDevicePath option;
  DM_LV_NAME: DmLvName option;
  DM_VG_NAME: DmVgName option;
}

/// The data received from a
/// udev block device remove event.
type RemoveEvent = {
  ACTION: Action;
  DEVLINKS: DevLinks option;
  DEVPATH: DevPath;
  MAJOR: Major;
  MINOR: Minor;
}

/// Data received from
/// a user command.
type SimpleEvent = {
  ACTION: Action;
}

let private object = function
  | Json.Object a -> Some (Map.ofArray a)
  | _ -> None

let private str = function
  | Json.String a -> Some a
  | _ -> None

let private unwrapString a =
    match a with
    | Json.String a -> a
    | _ -> failwith "Invalid JSON, it must be a string"

let private matchAction (name:Action) (x:Map<string, Json.Json>) =
  x
  |> Map.tryFind "ACTION"
  |> Option.bind str
  |> Option.map Action
  |> Option.filter((=) name)
  |> Option.map(fun _ -> x)

let private matchActions (names:Action list) (x:Map<string, Json.Json>) =
  x
  |> Map.tryFind "ACTION"
  |> Option.bind str
  |> Option.map Action
  |> Option.filter(fun x -> List.contains x names)
  |> Option.map(fun _ -> x)

let private findOrFail (key:string) x =
  match Map.tryFind key x with
    | Some(x) -> unwrapString x
    | None -> failwith (sprintf "Could not find key %s in %O" key x)

let private isOne = function
  | "1" -> true
  | _ -> false

let private emptyStrToNone x = if x = "" then None else Some(x)

let private findOrNone key x =
  x |> Map.tryFind key |> Option.bind str

let private intToIdPartEntryNumber = Option.map (int >> IdPartEntryNumber)

let private parseMajor = findOrFail "MAJOR" >> Major
let private parseMinor =  findOrFail "MINOR" >> Minor
let private parseDevlinks = findOrNone "DEVLINKS" >> Option.map DevLinks
let private parseDevName = findOrFail "DEVNAME" >> Path
let private parseDevPath = findOrFail "DEVPATH" >> DevPath
let private parseIdVendor = findOrNone "ID_VENDOR" >> Option.map IdVendor
let private parseIdModel = findOrNone "ID_MODEL" >> Option.map IdModel
let private parseIdSerial = findOrNone "ID_SERIAL" >> Option.map IdSerial
let private parseIdFsType = findOrNone "ID_FS_TYPE" >> Option.map IdFsType
let private parseIdPartEntryNumber = findOrNone "ID_PART_ENTRY_NUMBER" >> intToIdPartEntryNumber
let private parseImlSize = findOrNone "IML_SIZE" >> Option.map ImlSize
let private parseImlScsi80 = findOrNone "IML_SCSI_80" >> Option.map ImlScsi80
let private parseImlScsi83 = findOrNone "IML_SCSI_83" >> Option.map ImlScsi83
let private parseImlRo =
  findOrNone "IML_IS_RO"
  >> Option.map(isOne >> ImlIsRo)

let private parseDmMultipathDevicePath =
  findOrNone "DM_MULTIPATH_DEVICE_PATH"
  >> Option.map(isOne >> DmMultipathDevicePath)

let private parseDmLvName = findOrNone "DM_LV_NAME" >> Option.map DmLvName

let private parseDmVgName = findOrNone "DM_VG_NAME" >> Option.map DmVgName

let extractAddEvent x =
  let devType =
    x
      |> Map.find "DEVTYPE"
      |> unwrapString
      |> function
        | PartitionMatch (x) -> x
        | DiskMatch (x) -> x
        | _ -> failwith "DEVTYPE neither partition or disk"

  let paths (name:Path) = function
    | Some(DevLinks(links):DevLinks) ->
      let morePaths:Path array =
        links.Split(' ')
          |> Array.map((fun x -> x.Trim()) >> Path)

      Some(Array.concat [[| name |]; morePaths])
    | None -> None

  let devlinks = parseDevlinks x
  let devname = parseDevName x

  {
    ACTION = addAction;
    MAJOR = parseMajor x;
    MINOR = parseMinor x;
    DEVLINKS = devlinks;
    DEVNAME = devname;
    PATHS = paths devname devlinks;
    DEVPATH = parseDevPath x;
    DEVTYPE = devType;
    ID_VENDOR = parseIdVendor x;
    ID_MODEL = parseIdModel x;
    ID_SERIAL = parseIdSerial x;
    ID_FS_TYPE = parseIdFsType x |> Option.bind(fun (IdFsType(x):IdFsType) -> emptyStrToNone x) |> Option.map IdFsType;
    ID_PART_ENTRY_NUMBER = parseIdPartEntryNumber x
    IML_SIZE = parseImlSize x |> Option.bind(fun (ImlSize(x):ImlSize) -> emptyStrToNone x) |> Option.map ImlSize;
    IML_IS_RO = parseImlRo x
    IML_SCSI_80 = parseImlScsi80 x |> Option.map(fun (ImlScsi80(x)) -> x.Trim() |> ImlScsi80);
    IML_SCSI_83 = parseImlScsi83 x |> Option.map(fun (ImlScsi83(x)) -> x.Trim() |> ImlScsi83);
    DM_MULTIPATH_DEVICE_PATH = parseDmMultipathDevicePath x;
    DM_LV_NAME = parseDmLvName x;
    DM_VG_NAME = parseDmVgName x;
  }

let (|AddOrChangeEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchActions [addAction; changeAction])
    |> Option.map (extractAddEvent)

let (|RemoveEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction  (removeAction))
    |> Option.map (fun x ->
      {
        ACTION = removeAction;
        DEVLINKS = parseDevlinks x;
        DEVPATH = parseDevPath x;
        MAJOR = parseMajor x;
        MINOR = parseMinor x;
      }
    )

let (|InfoEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction (infoAction))
    |> Option.map (fun _ -> { ACTION = infoAction; })
