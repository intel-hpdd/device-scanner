// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec DeviceScannerDaemon.DeviceFns

open Fable.Core
open Fable
open Node.Net
open System.Collections.Generic
open UdevEventTypes.EventTypes

let deviceMap:IDictionary<string, Device> = dict[||]

[<Pojo>]
type Device = {
  paths: string array;
  ``type``: IDevType;
  path: string;
  dev_path: string;
  major_minor: string;
  serial_80: string option;
  serial_83: string option;
  size: int;
  filesystem_type: string;
  partition_number: int option
  parent_path: string;
  mutable parent: string option;
}

let getMajorMinor (x:IAdd):string =
  x.MAJOR + ":" + x.MINOR

let getSerial80 (x:IAdd) =
  match x.ID_VENDOR with
    | None -> None
    | Some(y) -> Some(y + x.ID_MODEL)

let getParentPath (x:string) =
  let xs = x.Split '/'
  let last = Array.length xs - 2
  xs.[0..last] |> String.concat "/"

let findByDevPath (devPath: string) (x:Device) =
  devPath = x.dev_path

let addHandler (x:IAdd) =
  let majorMinor = getMajorMinor x

  let trim (x:string) =
    x.Trim()

  let links = (
    x.DEVLINKS.Split ' '
    |> Array.map (fun x -> x.Trim())
    |> fun xs -> Array.append xs [|x.DEVNAME|]
  )

  let device = {
    paths = links;
    ``type`` = x.DEVTYPE;
    path = Array.head links;
    dev_path = x.DEVPATH;
    major_minor = majorMinor;
    serial_80 = getSerial80(x);
    serial_83 = x.ID_SERIAL;
    size = (Import.JS.parseInt x.IML_SIZE 10) * 512;
    filesystem_type = x.ID_FS_TYPE; // string might be empty
    partition_number = Option.map (fun x -> Import.JS.parseInt x 10) x.ID_PART_ENTRY_NUMBER;
    parent_path = getParentPath(x.DEVPATH);
    parent = None;
  }

  deviceMap.Add (majorMinor, device)

  let devices = deviceMap.Values

  let disks = (Seq.filter (fun x ->
    match x.``type`` with
      | Disk -> true
      | _ -> false
  ) devices)

  let partitions = (Seq.filter (fun x ->
    match x.``type`` with
      | Partition -> true
      | _ -> false
  ) devices)

  Seq.map (fun p ->
    let m = Seq.tryFind (findByDevPath p.parent_path) disks

    p.parent <- Option.map (fun x -> x.major_minor) m

    p
  ) partitions |> ignore

  ()

let dataHandler (c:net_types.Socket) = function
  | Info -> c.write(Fable.Core.JsInterop.toJson deviceMap) |> ignore
  | Add(x)  -> addHandler x
  | Remove(x) -> ()
