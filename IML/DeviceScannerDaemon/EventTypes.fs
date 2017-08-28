// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.UdevEventTypes.EventTypes

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

type AddEvent = {
  ACTION: string;
  MAJOR: string;
  MINOR: string;
  DEVLINKS: string;
  DEVNAME: string;
  DEVPATH: string;
  DEVTYPE: DevType;
  ID_VENDOR: string option;
  ID_MODEL: string;
  ID_SERIAL: string option;
  ID_FS_TYPE: string;
  ID_PART_ENTRY_NUMBER: string option;
  IML_SIZE: string option;
}

type RemoveEvent = {
  ACTION: string;
  DEVLINKS: string;
  DEVPATH: string;
  MAJOR: string;
  MINOR: string;
}

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

let private findOrFail key x =
  x |> Map.find key |> unwrapString

let private findOrNone key x =
  x |> Map.tryFind key |> Option.bind str

let private major = findOrFail "MAJOR"
let private minor =  findOrFail "MINOR"
let private devlinks = findOrFail "DEVLINKS"
let private devName = findOrFail "DEVNAME"
let private devPath = findOrFail "DEVPATH"
let private idVendor = findOrNone "ID_VENDOR"
let private idModel = findOrFail "ID_MODEL"
let private idSerial = findOrNone "ID_SERIAL"
let private idFsType = findOrFail "ID_FS_TYPE"
let private idPartEntryNumber = findOrNone "ID_PART_ENTRY_NUMBER"
let private imlSize = findOrNone "IML_SIZE"

let extractAddEvent x =
  let devType =
    x
      |> Map.find "DEVTYPE"
      |> unwrapString
      |> function
        | PartitionMatch (x) -> x
        | DiskMatch (x) -> x
        | _ -> failwith "DEVTYPE neither partition or disk"

  {
    ACTION = "add";
    MAJOR = major x;
    MINOR = minor x;
    DEVLINKS = devlinks x;
    DEVNAME = devName x;
    DEVPATH = devPath x;
    DEVTYPE = devType;
    ID_VENDOR = idVendor x;
    ID_MODEL = idModel x;
    ID_SERIAL = idSerial x;
    ID_FS_TYPE = idFsType x;
    ID_PART_ENTRY_NUMBER = idPartEntryNumber x;
    IML_SIZE = imlSize x;
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
        DEVLINKS = devlinks x;
        DEVPATH = devName x;
        MAJOR = major x;
        MINOR = minor x;
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
