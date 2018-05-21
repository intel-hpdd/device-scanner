// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec IML.Types.LegacyTypes

open Thoth.Json
open IML.Types.UeventTypes
open IML.CommonLibrary
open System.Text.RegularExpressions
open IML.Types.CommandTypes

let private encodeDict encoder m =
  m
    |> Map.map (fun _ y -> encoder y)
    |> Encode.dict

let private optional' t x = Decode.optional x (Decode.option t) None
let private optionalString x = optional' Decode.string x
let private optionalInt x = optional' Decode.int x
let private optionalBool x = optional' Decode.bool x
let private stringProp x = Decode.required x Decode.string


/// This type serves as a virtual tree (though right now it's assumed to be a graph).
/// There is a precedence that determines
/// canonical path. The precedence is:
///
///
///                  ┌────────────────────────────┐
///                  │           /dev/*           │
///                  └────────────────────────────┘
///                                 │
///                ┌────────────────┼────────────────┐
///                │                │                │
///                ▼                │                ▼
/// ┌────────────────────────────┐  │ ┌────────────────────────────┐
/// │    /dev/disk/by-path/*     │  │ │     /dev/disk/by-id/*      │
/// └────────────────────────────┘  │ └────────────────────────────┘
///                │                │                │
///                │                │                │
///                │                │                │
///                │                │                │
///                │                ▼                │
///                │ ┌────────────────────────────┐  │
///                └▶│       /dev/mapper/*        │◀─┘
///                  └────────────────────────────┘
///
///
/// Given the actual data structure is a map,
/// the user can lookup a node anywhere along the path,
/// and resolve it to it's furthest point along the tree.
///
/// Also, given the whole structure is built off of
/// known logic, and we have all the parts we need to
/// construct at each tick, we should remove this data
/// structure and determine the canonical path when needed by computing the
/// device nodes.
///
/// Even futher than that, we should not have a singular canonical path, but
/// preserve the aliases and allow any of them to be lookup keys.

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

        addNormalizedDevices devPaths diskByPathPaths t
          |> addNormalizedDevices devPaths diskByIdPaths
          |> addNormalizedDevices diskByPathPaths mapperPaths
          |> addNormalizedDevices diskByIdPaths mapperPaths

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
  size: string;
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
        ("size", Encode.string size);
        ("pvs_major_minor", Encode.array (encodeStrings pvs_major_minor));
      ]

  let encoder =
    encodeDict encode

  let decode =
    Decode.decode
      (fun name uuid size pvs_major_minor ->
        {
          name = name
          uuid = uuid
          size = size
          pvs_major_minor = pvs_major_minor
        } : Vg)
      |> stringProp "name"
      |> stringProp "uuid"
      |> stringProp "size"
      |> Decode.required "pvs_major_minor" (Decode.array Decode.string)


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

  let encoder (x:Map<string, Map<string, Lv>>) =
    encodeDict (encodeDict encode) x

  let decode =
    Decode.decode
      (fun name uuid size block_device ->
      {
        name = name
        uuid = uuid
        size = size
        block_device = block_device
      } : Lv)
    |> stringProp "name"
    |> stringProp "uuid"
    |> optionalString "size"
    |> stringProp "block_device"

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
        ("path", UEvent.pathValue path);
        ("block_device", Encode.string block_device);
        ("drives", Encode.array (UEvent.pathValues drives));
      ]

  let encoder =
    encodeDict encode

  let decode =
    Decode.decode
      (fun path block_device drives ->
      {
        path = path
        block_device = block_device
        drives = drives
      } : MdRaid)
    |> Decode.required "path" (Decode.map Path Decode.string)
    |> Decode.required "block_device" Decode.string
    |> Decode.required "drives" (Decode.array (Decode.map Path Decode.string))


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

  let encode
    ({
      major_minor = majorMinor;
      parent = parent;
      serial_83 = serial83;
      serial_80 = serial80;
      path = Path(path);
      size = size;
    }: MpathNode) =
      Encode.object [
        ("major_minor", Encode.string majorMinor);
        ("parent", Encode.option UEvent.devPathValue parent);
        ("serial_83", Encode.option Encode.string serial83);
        ("serial_80", Encode.option Encode.string serial80);
        ("path", Encode.string path);
        ("size", Encode.option Encode.string size);
      ]

  let decode =
    Decode.decode
      (fun major_minor parent serial_83 serial_80 path size ->
        ({
          major_minor = major_minor
          parent = parent
          serial_83 = serial_83
          serial_80 = serial_80
          path = path
          size = size
        })
      )
    |> stringProp "major_minor"
    |> optional' (Decode.map DevPath Decode.string) "parent"
    |> optionalString "serial_83"
    |> optionalString "serial_80"
    |> Decode.required "path" (Decode.map Path Decode.string)
    |> optionalString "size"


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

  let encode
    {
      name = name;
      block_device = blockDevice;
      nodes = nodes
    } =
      Encode.object [
        ("name", Encode.string name);
        ("block_device", Encode.string blockDevice);
        ("nodes", Encode.array (Array.map MpathNode.encode nodes));
      ]

  let encoder =
    encodeDict encode

  let decode: obj -> Result<Mpath, Decode.DecoderError> =
    Decode.decode
      (fun name block_device nodes ->
        ({
          name = name
          block_device = block_device
          nodes = nodes
        })
      )
    |> stringProp "name"
    |> stringProp "blockDevice"
    |> Decode.required "nodes" (Decode.array (MpathNode.decode))


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

  let encoder =
    encodeDict encode

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
  is_zfs_reserved: bool;
  parent: string option;
  dm_multipath: bool option;
  dm_lv: string option;
  dm_vg: string option;
  lv_uuid: string option;
  dm_slave_mms: string [];
  dm_vg_size: string option;
  vg_uuid: string option;
  md_uuid: string option;
  md_device_paths: string [];
}

module LegacyBlockDev =
  let parentByMajorMinor (b:BlockDevices) =
    Option.map (fun d -> b |> Map.find d |> UEvent.majorMinor)

  let ofUEvent (b:BlockDevices) (x:UEvent) =
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
      is_zfs_reserved = x.zfsReserved;
      parent = parentByMajorMinor b x.parent;
      dm_multipath = x.dmMultipathDevpath;
      dm_lv = x.dmLvName;
      dm_vg = x.dmVgName;
      lv_uuid = x.lvUuid;
      dm_slave_mms = x.dmSlaveMMs;
      dm_vg_size = x.dmVgSize;
      vg_uuid = x.vgUuid
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
      is_zfs_reserved = is_zfs_reserved;
      parent = parent;
      dm_multipath = dm_multipath;
      dm_lv = dm_lv;
      dm_vg = dm_vg;
      lv_uuid = lv_uuid;
      dm_slave_mms = dm_slave_mms;
      dm_vg_size = dm_vg_size;
      vg_uuid = vg_uuid;
      md_uuid = md_uuid;
      md_device_paths = md_device_paths;
    } =
      Encode.object [
        ("major_minor", Encode.string major_minor);
        ("path", UEvent.pathValue path);
        ("paths", Encode.array (UEvent.pathValues paths));
        ("serial_80", Encode.option Encode.string serial_80);
        ("serial_83", Encode.option Encode.string serial_83);
        ("size", Encode.option Encode.string size);
        ("filesystem_type", Encode.option Encode.string filesystem_type);
        ("filesystem_usage", Encode.option Encode.string filesystem_usage);
        ("device_type", Encode.string device_type);
        ("device_path", UEvent.devPathValue device_path);
        ("partition_number", Encode.option Encode.int partition_number);
        ("is_ro", Encode.option Encode.bool is_ro);
        ("is_zfs_reserved", Encode.bool is_zfs_reserved);
        ("parent", Encode.option Encode.string parent);
        ("dm_multipath", Encode.option Encode.bool dm_multipath);
        ("dm_lv", Encode.option Encode.string dm_lv);
        ("lv_uuid", Encode.option Encode.string lv_uuid);
        ("dm_vg", Encode.option Encode.string dm_vg);
        ("vg_uuid", Encode.option Encode.string vg_uuid);
        ("dm_slave_mms", Encode.array (encodeStrings dm_slave_mms));
        ("dm_vg_size", Encode.option Encode.string dm_vg_size);
        ("md_uuid", Encode.option Encode.string md_uuid);
        ("md_device_paths", Encode.array (encodeStrings md_device_paths));
      ]

  let encoder =
    encodeDict encode

  let decode =
    Decode.decode
      (fun major_minor path paths serial_80 serial_83
           size filesystem_type filesystem_usage
           device_type device_path partition_number
           is_ro is_zfs_reserved parent dm_multipath
           dm_lv dm_vg lv_uuid vg_uuid dm_slave_mms
           dm_vg_size md_uuid md_device_paths ->
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
          is_zfs_reserved = is_zfs_reserved
          parent = parent
          dm_multipath = dm_multipath
          dm_lv = dm_lv
          dm_vg = dm_vg
          lv_uuid = lv_uuid
          vg_uuid = vg_uuid
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
      |> Decode.required "is_zfs_reserved" Decode.bool
      |> Decode.required "parent" (Decode.option Decode.string)
      |> optionalBool "dm_multipath"
      |> optionalString "dm_lv"
      |> optionalString "dm_vg"
      |> optionalString "lv_uuid"
      |> optionalString "vg_uuid"
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

  let decode =
    Decode.oneOf [
      (Decode.map LegacyDev.LegacyBlockDev LegacyBlockDev.decode);
      (Decode.map LegacyDev.LegacyZFSDev LegacyZFSDev.decode)
    ]

  let encode = function
    | LegacyBlockDev x -> LegacyBlockDev.encode x
    | LegacyZFSDev x -> LegacyZFSDev.encode x

  let encoder x =
    x
      |> Map.toList
      |> List.map (fun (x, y) ->
           (x, encode y)
      )
      |> Encode.object

let private localFsEncoder (x:Map<string,(string * string)>) =
  let encode (a, b) =
    [| a; b |]
      |> encodeStrings
      |> Encode.array

  encodeDict encode x

let private localFSDecoder =
  (Decode.map2
    (fun x y ->
      (x, y)
    )
    (Decode.field "0" Decode.string)
    (Decode.field "1" Decode.string))


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
      mpath = mpath;
    } =
      Encode.object [
        ("devs", LegacyDev.encoder devs)
        ("lvs", Lv.encoder lvs)
        ("vgs", Vg.encoder vgs)
        ("mds", MdRaid.encoder mds)
        ("zfspools", LegacyZFSDev.encoder zfspools)
        ("zfsdatasets", LegacyZFSDev.encoder zfsdatasets)
        ("local_fs", localFsEncoder localFs)
        ("mpath", Mpath.encoder mpath)
      ]

  let encoder =
    encode
      >> Encode.encode 0

  let decode: obj -> Result<LegacyDevTree, Decode.DecoderError> =
    Decode.decode
      (fun devs lvs vgs mds zfspools zfsdatasets local_fs mpath ->
          ({
            devs = devs
            lvs = lvs
            vgs = vgs
            mds = mds
            zfspools = zfspools
            zfsdatasets = zfsdatasets
            local_fs = local_fs
            mpath = mpath
          })
        )
          |> Decode.required "devs" (Decode.dict LegacyDev.decode)
          |> Decode.required "lvs" (Decode.dict (Decode.dict Lv.decode))
          |> Decode.required "vgs" (Decode.dict Vg.decode)
          |> Decode.required "mds" (Decode.dict MdRaid.decode)
          |> Decode.required "zfspools" (Decode.dict LegacyZFSDev.decode)
          |> Decode.required "zfsdatasets" (Decode.dict LegacyZFSDev.decode)
          |> Decode.required "local_fs" (Decode.dict localFSDecoder)
          |> Decode.required "mpath" (Decode.dict Mpath.decode)
