// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec IML.Types.LegacyTypes

open Thoth.Json
open IML.Types.UeventTypes
open IML.CommonLibrary
open System.Text.RegularExpressions

let private pathValue (Path x) = Encode.string x
let private pathValues paths = paths |> Array.map pathValue
let private devPathValue (DevPath x) = Encode.string x
let private encodeStrings xs = Array.map Encode.string xs

let private optional' t x = Decode.optional x (Decode.option t) None
let private optionalString x = optional' Decode.string x
let private optionalInt x = optional' Decode.int x
let private optionalBool x = optional' Decode.bool x

type NormalizedDeviceTable = Map<Path, Path>

module NormalizedDeviceTable =
  let private filterByRegex r (Path(p)) =
    Regex.Match(p, r).Success

  let addNormalizedDevices xs ys (m:NormalizedDeviceTable): NormalizedDeviceTable =
    Array.fold (fun m x ->
      ys
        |> Array.filter(fun y -> y <> x)
        |> Array.fold (fun m y -> Map.add x y m) m
    ) m xs

  let create (m:Map<string, LegacyBlockDev>) =
    let xs =
      m
        |> Map.toList
        |> List.map snd

    xs
      |> List.fold (fun t x ->
        let paths = x.paths

        let devPaths =
          Array.filter (filterByRegex UEvent.devPathRegex) paths

        let diskByIdPaths =
          Array.filter (filterByRegex UEvent.diskByIdRegex) paths

        let diskByPathPaths =
          Array.filter (filterByRegex UEvent.diskByPathRegex) paths

        let mapperPaths =
          Array.filter (filterByRegex UEvent.mapperPathRegex) paths

        let table =
          addNormalizedDevices devPaths diskByPathPaths t
            |> addNormalizedDevices devPaths diskByIdPaths
            |> addNormalizedDevices diskByPathPaths mapperPaths
            |> addNormalizedDevices diskByIdPaths mapperPaths

        xs
          |> List.filter (fun v ->
            v.dm_uuid
              |> Option.filter(fun x -> x.StartsWith("mpath-"))
              |> Option.isSome
          )
          |> List.collect (fun v ->
            let r = Array.filter (filterByRegex UEvent.mapperPathRegex) x.paths

            v.dm_slave_mms
              |> List.ofArray
              |> List.map (fun x ->
                let dev =
                  Map.find x m

                let paths =
                  dev.paths
                    |> Array.filter (filterByRegex UEvent.diskByIdRegex)

                (paths, r)
              )
          )
          |> List.fold (fun t (l, r) ->
            addNormalizedDevices l r t
          ) table

    ) Map.empty

  let normalizedDevicePath t p =
    let mutable visited = Set.empty

    let rec findPath p =
      if Set.contains p visited then
        p
      else
        match Map.tryFind p t with
          | Some x ->
            visited <- Set.add x visited
            findPath x
          | None -> p

    findPath p


type Vg = {
  name: string;
  uuid: string;
  size: int;
  pvs_major_minor: string [];
}

module Vg =
  let encode
    {
      name = name;
      uuid = uuid;
      size = size;
      pvs_major_minor = pvs_major_minor;
    } =
      Encode.object [
        ("name", Encode.string name);
        ("uuid", Encode.string uuid);
        ("size", Encode.int size);
        ("pvs_major_minor", Encode.array (encodeStrings pvs_major_minor));
      ]

  let encoder x =
    x
      |> Map.toList
      |> List.map (fun (x, y) ->
           (x, encode y)
      )
      |> Encode.object


type Lv = {
  name: string;
  uuid: string;
  size: string option;
  block_device: string;
}

module Lv =
  let encode
    ({
      name = name;
      uuid = uuid;
      size = size;
      block_device = block_device;
    }:Lv) =
      Encode.object [
        ("name", Encode.string name);
        ("uuid", Encode.string uuid);
        ("size", Encode.option Encode.string size);
        ("block_device", Encode.string block_device);
      ]

  let encoder' x =
    x
      |> Map.toList
      |> List.map (fun (x, y) ->
           (x, encode y)
      )
      |> Encode.object

  let encoder (x:Map<string, Map<string, Lv>>) =
    x
      |> Map.toList
      |> List.map (fun (x, y) ->
           (x, encoder' y)
      )
      |> Encode.object



type MdRaid = {
  path: Path;
  block_device: string;
  drives: Path [];
}

module MdRaid =
  let encode
    ({
      path = path;
      block_device = block_device;
      drives = drives;
    }:MdRaid) =
      Encode.object [
        ("path", pathValue path);
        ("block_device", Encode.string block_device);
        ("drives", Encode.array (pathValues drives));
      ]

  let encoder x =
    x
      |> Map.toList
      |> List.map (fun (x, y) ->
           (x, encode y)
      )
      |> Encode.object


type MpathNode = {
  major_minor: string;
  parent: DevPath option;
  serial_83: string option;
  serial_80: string option;
  path: Path;
  size: string option;
}

module MpathNode =
  let ofUEvent (x:UEvent) =
    {
      major_minor = UEvent.majorMinor x;
      parent = x.parent;
      serial_83 = x.scsi83;
      serial_80 = x.scsi80;
      path = Array.head x.paths;
      size = x.size
    }

type Mpath = {
  name: string;
  block_device: string;
  nodes: MpathNode [];
}

module Mpath =
  let ofBlockDevices (xs:BlockDevices) =
    xs
      |> Map.toArray
      |> Array.map snd
      |> Array.filter (fun v ->
        v.isMpath
      )
      |> Array.map
        (fun v ->
          let x =
            {
              name =
                v.dmName
                  |> Option.expect "Mpath device did not have a name";
              block_device = UEvent.majorMinor v;
              nodes =
                v.dmSlaveMMs
                  |> Array.choose (BlockDevices.tryFindByMajorMinor xs)
                  |> Array.map MpathNode.ofUEvent
            }

          (x.name, x)
        )
        |> Map.ofArray

  let addToNdt mpaths ndt =
    Map.fold
      (fun state k (v:Mpath) ->
        Array.fold (fun ndt (x:MpathNode) ->
          NormalizedDeviceTable.addNormalizedDevices [| x.path |]  [| Path (sprintf "/dev/mapper/%s" k) |] ndt
        ) state v.nodes

      ) ndt mpaths


type LegacyZFSDev = {
  name: string;
  path: string;
  block_device: string;
  uuid: string;
  size: string;
  drives: string [];
}

module LegacyZFSDev =
  let encode
    {
      name = name;
      path = path;
      block_device = block_device;
      uuid = uuid;
      size = size;
      drives = drives;
    } =
      Encode.object [
        ("name", Encode.string name);
        ("path", Encode.string path);
        ("block_device", Encode.string block_device);
        ("uuid", Encode.string uuid);
        ("size", Encode.string size);
        ("drives", Encode.array (encodeStrings drives));
      ]

  let encoder x =
    x
      |> Map.toList
      |> List.map (fun (x, y) ->
           (x, encode y)
      )
      |> Encode.object

  let decode =
    Decode.decode
      (fun name path block_device uuid size drives ->
        {
          name = name
          path = path
          block_device = block_device
          uuid = uuid
          size = size
          drives = drives
        }
      )
      |> Decode.required "name" Decode.string
      |> Decode.required "path" Decode.string
      |> Decode.required "block_device" Decode.string
      |> Decode.required "uuid" Decode.string
      |> Decode.required "size" Decode.string
      |> Decode.required "drives" (Decode.array Decode.string)

  let decoder =
    Decode.decodeString decode
      >> Result.mapError exn


type LegacyBlockDev = {
  major_minor: string;
  path: Path;
  paths: Path [];
  serial_80: string option;
  serial_83: string option;
  size: string option;
  filesystem_type: string option;
  filesystem_usage: string option;
  device_type: string;
  device_path: DevPath;
  partition_number: int option;
  is_ro: bool option;
  parent: string option;
  dm_multipath: bool option;
  dm_lv: string option;
  dm_vg: string option;
  dm_uuid: string option;
  dm_slave_mms: string [];
  dm_vg_size: string option;
  md_uuid: string option;
  md_device_paths: string [];
}

module LegacyBlockDev =
  let ofUEvent (x:UEvent) =
    {
      major_minor = UEvent.majorMinor x;
      path = Array.head x.paths;
      paths = x.paths;
      serial_80 = x.scsi80;
      serial_83 = x.scsi83;
      size = x.size;
      filesystem_type = x.fsType;
      filesystem_usage = x.fsUsage;
      device_type = x.devtype;
      device_path = x.devpath;
      partition_number = x.partEntryNumber;
      is_ro = x.readOnly;
      parent = None;
      dm_multipath = x.dmMultipathDevpath;
      dm_lv = x.dmLvName;
      dm_vg = x.dmVgName;
      dm_uuid = x.dmUUID;
      dm_slave_mms = x.dmSlaveMMs;
      dm_vg_size = x.dmVgSize;
      md_uuid = x.mdUUID;
      md_device_paths = x.mdDevs;
    }

  let encode
    {
      major_minor = major_minor;
      path = path;
      paths = paths;
      serial_80 = serial_80;
      serial_83 = serial_83;
      size = size;
      filesystem_type = filesystem_type;
      filesystem_usage = filesystem_usage;
      device_type = device_type;
      device_path = device_path;
      partition_number = partition_number;
      is_ro = is_ro;
      parent = parent;
      dm_multipath = dm_multipath;
      dm_lv = dm_lv;
      dm_vg = dm_vg;
      dm_uuid = dm_uuid;
      dm_slave_mms = dm_slave_mms;
      dm_vg_size = dm_vg_size;
      md_uuid = md_uuid;
      md_device_paths = md_device_paths;
    } =
      Encode.object [
        ("major_minor", Encode.string major_minor);
        ("path", pathValue path);
        ("paths", Encode.array (pathValues paths));
        ("serial_80", Encode.option Encode.string serial_80);
        ("serial_83", Encode.option Encode.string serial_83);
        ("size", Encode.option Encode.string size);
        ("filesystem_type", Encode.option Encode.string filesystem_type);
        ("filesystem_usage", Encode.option Encode.string filesystem_usage);
        ("device_type", Encode.string device_type);
        ("device_path", devPathValue device_path);
        ("partition_number", Encode.option Encode.int partition_number);
        ("is_ro", Encode.option Encode.bool is_ro);
        ("parent", Encode.option Encode.string parent);
        ("dm_multipath", Encode.option Encode.bool dm_multipath);
        ("dm_lv", Encode.option Encode.string dm_lv);
        ("dm_vg", Encode.option Encode.string dm_vg);
        ("dm_uuid", Encode.option Encode.string dm_uuid);
        ("dm_slave_mms", Encode.array (encodeStrings dm_slave_mms));
        ("dm_vg_size", Encode.option Encode.string dm_vg_size);
        ("md_uuid", Encode.option Encode.string md_uuid);
        ("md_device_paths", Encode.array (encodeStrings md_device_paths));
      ]

  let encoder x =
    x
      |> Map.toList
      |> List.map (fun (x, y) ->
           (x, encode y)
      )
      |> Encode.object

  let decode =
    Decode.decode
      (fun major_minor path paths serial_80 serial_83
           size filesystem_type filesystem_usage
           device_type device_path partition_number
           is_ro parent dm_multipath dm_lv dm_vg
           dm_uuid dm_slave_mms dm_vg_size md_uuid
           md_device_paths ->
        ({
          major_minor = major_minor
          path = path
          paths = paths
          serial_80 = serial_80
          serial_83 = serial_83
          size = size
          filesystem_type = filesystem_type
          filesystem_usage = filesystem_usage
          device_type = device_type
          device_path = device_path
          partition_number = partition_number
          is_ro = is_ro
          parent = parent
          dm_multipath = dm_multipath
          dm_lv = dm_lv
          dm_vg = dm_vg
          dm_uuid = dm_uuid
          dm_slave_mms = dm_slave_mms
          dm_vg_size = dm_vg_size
          md_uuid = md_uuid
          md_device_paths = md_device_paths
        })
      )
      |> Decode.required "major_minor" Decode.string
      |> Decode.required "path" (Decode.map Path Decode.string)
      |> Decode.required "paths" (Decode.array (Decode.map Path Decode.string))
      |> optionalString "serial_80"
      |> optionalString "serial_83"
      |> optionalString "size"
      |> optionalString "filesystem_type"
      |> optionalString "filesystem_usage"
      |> Decode.required "device_type" Decode.string
      |> Decode.required "device_path" (Decode.map DevPath Decode.string)
      |> optionalInt "partition_number"
      |> optionalBool "is_ro"
      |> optionalString "parent"
      |> optionalBool "dm_multipath"
      |> optionalString "dm_lv"
      |> optionalString "dm_vg"
      |> optionalString "dm_uuid"
      |> Decode.required "dm_slave_mms" (Decode.array Decode.string)
      |> optionalString "dm_vg_size"
      |> optionalString "md_uuid"
      |> Decode.required "md_device_paths" (Decode.array Decode.string)

  let decoder =
    Decode.decodeString decode
      >> Result.mapError exn


type LegacyDev =
  | LegacyBlockDev of LegacyBlockDev
  | LegacyZFSDev of LegacyZFSDev

module LegacyDev =

  let encode = function
    | LegacyBlockDev x ->
      Encode.object [ ("legacyblockdev", LegacyBlockDev.encode x) ]
    | LegacyZFSDev x ->
      Encode.object [ ("legacyzfsdev", LegacyZFSDev.encode x) ]

  let encoder x =
    x
      |> Map.toList
      |> List.map (fun (x, y) ->
           (x, encode y)
      )
      |> Encode.object

let private localFsEncoder (x:Map<string,(string * string)>) =
  let encode y =
    let (a, b) = y
    Encode.object [ ("a", Encode.string a); ("b", Encode.string b) ]
  x
    |> Map.toList
    |> List.map (fun (x, y) ->
         (x, encode y)
      )
    |> Encode.object

type LegacyDevTree = {
  devs: Map<string, LegacyDev>
  lvs: Map<string, Map<string, Lv>>
  vgs: Map<string, Vg>
  mds: Map<string, MdRaid>
  zfspools: Map<string, LegacyZFSDev>
  zfsdatasets: Map<string, LegacyZFSDev>
  local_fs: Map<string, (string * string)>
  mpath: Map<string, Mpath>;
}

module LegacyDevTree =

  let encode
    {
      devs = devs;
      lvs = lvs;
      vgs = vgs;
      mds = mds;
      zfspools = zfspools;
      zfsdatasets = zfsdatasets;
      local_fs = localFs;
    } =
      Encode.object [
        ("devs", LegacyDev.encoder devs)
        ("lvs", Lv.encoder lvs)
        ("vgs", Vg.encoder vgs)
        ("mds", MdRaid.encoder mds)
        ("zfspools", LegacyZFSDev.encoder zfspools)
        ("zfsdatasets", LegacyZFSDev.encoder zfsdatasets)
        ("local_fs", localFsEncoder localFs)
      ]

  let encoder =
    encode
      >> Encode.encode 0
