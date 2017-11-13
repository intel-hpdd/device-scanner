// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.EventTypes

open Fable.PowerPack
open Json
open Fable.Core
open IML.JsonDecoders
open IML.StringUtils
open IML.Maybe

let private hasPair k v m =
  m
    |> Map.tryFindKey (fun k' v' -> k = k' && v = v')
    |> Option.isSome

let hasAction v =
  hasPair "ACTION" (String v)

[<Erase>]
type DevPath = DevPath of string
[<Erase>]
type Path = Path of string

let (|Partition|Disk|) =
  function
    | x when x = "partition" -> Partition x
    | x when x = "disk" -> Disk x
    | _ -> failwith "DEVTYPE neither partition or disk"

/// The data emitted after processing a
/// udev block device add event
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
  ID_PART_ENTRY_NUMBER: int option;
  IML_SIZE: string option;
  IML_SCSI_80: string option;
  IML_SCSI_83: string option;
  IML_IS_RO: bool option;
  IML_DM_SLAVE_MMS: string [] option;
  IML_DM_VG_SIZE: string option;
  DM_MULTIPATH_DEVICE_PATH: bool option;
  DM_LV_NAME: string option;
  DM_VG_NAME: string option;
  DM_UUID: string option;
}

let private isOne = function
  | "1" -> true
  | _ -> false

let private emptyStrToNone x = if x = "" then None else Some(x)

let private findStr = findJson unwrapString

let private tryFindStr = tryFindJson str

let private parseDevName = findStr "DEVNAME" >> Path
let private parseDevPath = findStr "DEVPATH" >> DevPath

let extractAddEvent x =
  let devType =
    match findStr "DEVTYPE" x with
      | Disk(x) | Partition(x) -> x

  let inline paths name (xs:string option) =
    maybe {
      let! links = xs

      return name :: [ for x in links.Split(' ') -> x |> trim |> Path]
        |> List.toArray
    }

  let devlinks = tryFindStr "DEVLINKS" x
  let devname = parseDevName x

  {
    MAJOR = findStr "MAJOR" x;
    MINOR = findStr "MINOR" x;
    DEVNAME = devname;
    PATHS = paths devname devlinks;
    DEVPATH = parseDevPath x;
    DEVTYPE = devType;
    ID_VENDOR = tryFindStr "ID_VENDOR" x;
    ID_MODEL = tryFindStr "ID_MODEL" x;
    ID_SERIAL = tryFindStr "ID_SERIAL" x;
    ID_FS_TYPE = tryFindStr "ID_FS_TYPE" x |> Option.bind(emptyStrToNone);
    ID_PART_ENTRY_NUMBER = tryFindStr "ID_PART_ENTRY_NUMBER" x |> Option.map (int)
    IML_SIZE = tryFindStr "IML_SIZE" x |> Option.bind(emptyStrToNone);
    IML_IS_RO = tryFindStr "IML_IS_RO" x |> Option.map(isOne)
    IML_SCSI_80 = tryFindStr "IML_SCSI_80" x |> Option.map(trim);
    IML_SCSI_83 = tryFindStr "IML_SCSI_83" x |> Option.map(trim);
    IML_DM_SLAVE_MMS = tryFindStr "IML_DM_SLAVE_MMS" x
      |> Option.map(split [| ' ' |])
      |> Option.map(Array.map(trim));
    IML_DM_VG_SIZE = tryFindStr "IML_DM_VG_SIZE" x |> Option.map(trim);
    DM_MULTIPATH_DEVICE_PATH = tryFindStr "DM_MULTIPATH_DEVICE_PATH" x |> Option.map(isOne);
    DM_LV_NAME = tryFindStr "DM_LV_NAME" x;
    DM_VG_NAME = tryFindStr "DM_VG_NAME" x;
    DM_UUID = tryFindStr "DM_UUID" x;
  }

let (|UdevAdd|_|) =
  function
    | x when hasAction "add" x -> Some (extractAddEvent x)
    | _ -> None

let (|UdevChange|_|) =
  function
    | x when hasAction "change" x -> Some (extractAddEvent x)
    | _ -> None

let (|UdevRemove|_|) =
  function
    | x when hasAction "remove" x -> Some (parseDevPath x)
    | _ -> None
