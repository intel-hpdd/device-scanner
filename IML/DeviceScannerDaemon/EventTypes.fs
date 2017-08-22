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
  ID_FS_TYPE: string option;
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

type InfoEvent = {
  ACTION: string;
}

let private object = function
  | Json.Object a -> Some (Map.ofArray a)
  | _ -> None

let private str = function
  | Json.String a -> Some a
  | _ -> None

let unwrapString a =
    match a with
    | Json.String a -> a
    | _ -> failwith "Invalid JSON, it must be a string"

let private matchAction (name:string) (x:Map<string, Json.Json>) =
  x
  |> Map.tryFind "ACTION"
  |> Option.bind str
  |> Option.filter((=) name)
  |> Option.map(fun _ -> x)

let (|InfoEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction "info")
    |> Option.map (fun _ -> { ACTION = "info"; })

let findOrFail key x =
  x |> Map.find key |> unwrapString

let findOrNone key x =
  x |> Map.tryFind key |> Option.bind str

let (|AddEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction "add")
    |> Option.map (fun x ->
      let major = findOrFail "MAJOR" x
      let minor =  findOrFail "MINOR" x
      let devlinks = findOrFail "DEVLINKS" x
      let devName = findOrFail "DEVNAME" x
      let devPath = findOrFail "DEVPATH" x
      let idVendor = findOrNone "ID_VENDOR" x
      let idModel = findOrFail "ID_MODEL" x
      let idSerial = findOrNone "ID_SERIAL" x
      let idFsType = findOrNone "ID_FS_TYPE" x // Investigate this
      let idPartEntryNumber = findOrNone "ID_PART_ENTRY_NUMBER" x
      let imlSize = findOrNone "IML_SIZE" x // Investigate this too

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
        MAJOR = major;
        MINOR = minor;
        DEVLINKS = devlinks;
        DEVNAME = devName;
        DEVPATH = devPath;
        DEVTYPE = devType;
        ID_VENDOR = idVendor;
        ID_MODEL = idModel;
        ID_SERIAL = idSerial;
        ID_FS_TYPE = idFsType;
        ID_PART_ENTRY_NUMBER = idPartEntryNumber;
        IML_SIZE = imlSize;
      }
    )

let (|RemoveEventMatch|_|) x =
  x
    |> object
    |> Option.bind (matchAction  "remove")
    |> Option.map (fun x ->
      let devlinks = x |> Map.find "DEVLINKS" |> unwrapString
      let devName = x |> Map.find "DEVNAME" |> unwrapString
      let major = x |> Map.find "MAJOR" |> unwrapString
      let minor = x |> Map.find "MINOR" |> unwrapString

      {
        ACTION = "remove";
        DEVLINKS = devlinks;
        DEVPATH = devName;
        MAJOR = major;
        MINOR = minor;
      }
    )
