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
type DevPath = DevPath of string

/// The data received from a
/// udev block device add event
type AddEvent = {
  ACTION: string;
  MAJOR: string;
  MINOR: string;
  DEVLINKS: string option;
  PATHS: string array option;
  DEVNAME: string;
  DEVPATH: DevPath;
  DEVTYPE: DevType;
  ID_VENDOR: string option;
  ID_MODEL: string option;
  ID_SERIAL: string option;
  ID_FS_TYPE: string option;
  ID_PART_ENTRY_NUMBER: int option;
  IML_SIZE: string option;
  IML_SCSI_80: string option;
  IML_SCSI_83: string option;
  IML_IS_RO: bool option;
}

/// The data received from a
/// udev block device remove event.
type RemoveEvent = {
  ACTION: string;
  DEVLINKS: string option;
  DEVPATH: DevPath;
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

let private matchActions (names:string list) (x:Map<string, Json.Json>) =
  x
  |> Map.tryFind "ACTION"
  |> Option.bind str
  |> Option.filter(fun x -> List.contains x names)
  |> Option.map(fun _ -> x)

let private findOrFail (key:string) x =
  match Map.tryFind key x with
    | Some(x) -> unwrapString x
    | None -> failwith (sprintf "Could not find key %s in %O" key x)

let private emptyStrToNone x = if x = "" then None else Some(x)

let private trimOpt = Option.map(fun (x:string) -> x.Trim())

let private findOrNone key x =
  x |> Map.tryFind key |> Option.bind str

let private parseMajor = findOrFail "MAJOR"
let private parseMinor =  findOrFail "MINOR"
let private parseDevlinks = findOrNone "DEVLINKS"
let private parseDevName = findOrFail "DEVNAME"
let private parseDevPath x = findOrFail "DEVPATH" x |> DevPath
let private parseIdVendor = findOrNone "ID_VENDOR"
let private parseIdModel = findOrNone "ID_MODEL"
let private parseIdSerial = findOrNone "ID_SERIAL"
let private parseIdFsType = findOrNone "ID_FS_TYPE"
let private parseIdPartEntryNumber = findOrNone "ID_PART_ENTRY_NUMBER"
let private parseImlSize = findOrNone "IML_SIZE"
let private parseImlScsi80 = findOrNone "IML_SCSI_80"
let private parseImlScsi83 = findOrNone "IML_SCSI_83"
let private parseImlRo = findOrNone "IML_IS_RO"

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

  let imlRo =
    x
      |> parseImlRo
      |> Option.map(function
        | "1" -> true
        | _ -> false
      )

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
    ID_FS_TYPE = parseIdFsType x |> Option.bind(emptyStrToNone);
    ID_PART_ENTRY_NUMBER = parseIdPartEntryNumber x |> Option.map(int);
    IML_SIZE = parseImlSize x |> Option.bind(emptyStrToNone);
    IML_IS_RO = imlRo;
    IML_SCSI_80 = parseImlScsi80 x |> trimOpt;
    IML_SCSI_83 = parseImlScsi83 x |> trimOpt;
  }

let (|AddOrChangeEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchActions ["add"; "change"])
    |> Option.map (extractAddEvent)

let (|RemoveEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction  "remove")
    |> Option.map (fun x ->
      {
        ACTION = "remove";
        DEVLINKS = parseDevlinks x;
        DEVPATH = parseDevPath x;
        MAJOR = parseMajor x;
        MINOR = parseMinor x;
      }
    )

let (|InfoEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction "info")
    |> Option.map (fun _ -> { ACTION = "info"; })
