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

/// The data received from a
/// udev block device add event
type AddEvent = {
  ACTION: string;
  MAJOR: string;
  MINOR: string;
  DEVLINKS: string option;
  PATHS: string array option;
  DEVNAME: string;
  DEVPATH: string;
  DEVTYPE: DevType;
  ID_VENDOR: string option;
  ID_MODEL: string option;
  ID_SERIAL: string option;
  ID_FS_TYPE: string option;
  ID_PART_ENTRY_NUMBER: string option;
  IML_SIZE: string option;
  IML_SCSI_80: string option;
  IML_SCSI_83: string option;
}

/// The data received from a
/// udev block device remove event.
type RemoveEvent = {
  ACTION: string;
  DEVLINKS: string option;
  DEVPATH: string;
  MAJOR: string;
  MINOR: string;
}

/// Data received from
/// a user command.
type SimpleEvent = {
  ACTION: string;
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

let private matchAction (name:string) (x:Map<string, Json.Json>) =
  x
  |> Map.tryFind "ACTION"
  |> Option.bind str
  |> Option.filter((=) name)
  |> Option.map(fun _ -> x)

let private findOrFail (key:string) x =
  match Map.tryFind key x with
    | Some(x) -> unwrapString x
    | None -> failwith (sprintf "Could not find key %s in %O" key x)

let trimOpt = Option.map(fun (x:string) -> x.Trim())

let private findOrNone key x =
  x |> Map.tryFind key |> Option.bind str

let private parseMajor = findOrFail "MAJOR"
let private parseMinor =  findOrFail "MINOR"
let private parseDevlinks = findOrNone "DEVLINKS"
let private parseDevName = findOrFail "DEVNAME"
let private parseDevPath = findOrFail "DEVPATH"
let private parseIdVendor = findOrNone "ID_VENDOR"
let private parseIdModel = findOrNone "ID_MODEL"
let private parseIdSerial = findOrNone "ID_SERIAL"
let private parseIdFsType = findOrNone "ID_FS_TYPE"
let private parseIdPartEntryNumber = findOrNone "ID_PART_ENTRY_NUMBER"
let private parseImlSize = findOrNone "IML_SIZE"
let private parseImlScsi80 = findOrNone "IML_SCSI_80"
let private parseImlScsi83 = findOrNone "IML_SCSI_83"

let extractAddEvent x =
  let devType =
    x
      |> Map.find "DEVTYPE"
      |> unwrapString
      |> function
        | PartitionMatch (x) -> x
        | DiskMatch (x) -> x
        | _ -> failwith "DEVTYPE neither partition or disk"

  let paths (name:string) = function
    | Some(links:string) ->
      let morePaths =
        links.Split(' ')
          |> Array.map(fun x -> x.Trim())

      Some(Array.concat [[| name |]; morePaths])
    | None -> None

  let devlinks = parseDevlinks x
  let devname = parseDevName x

  {
    ACTION = "add";
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
    ID_FS_TYPE = parseIdFsType x;
    ID_PART_ENTRY_NUMBER = parseIdPartEntryNumber x;
    IML_SIZE = parseImlSize x;
    IML_SCSI_80 = parseImlScsi80 x |> trimOpt;
    IML_SCSI_83 = parseImlScsi83 x |> trimOpt;
  }

let (|AddEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction "add")
    |> Option.map (extractAddEvent)

let (|RemoveEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction  "remove")
    |> Option.map (fun x ->
      {
        ACTION = "remove";
        DEVLINKS = parseDevlinks x;
        DEVPATH = parseDevName x;
        MAJOR = parseMajor x;
        MINOR = parseMinor x;
      }
    )

let (|ReadEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction "read")
    |> Option.map (fun _ -> { ACTION = "read"; })

let (|InfoEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction "info")
    |> Option.map (fun _ -> { ACTION = "info"; })
