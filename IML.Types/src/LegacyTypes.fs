// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.LegacyTypes

open Thoth.Json
open IML.Types.UeventTypes


let private pathValue (Path x) =
  Encode.string x

let private pathValues paths =
  paths
    |> Array.map pathValue

let private devPathValue (DevPath x) =
  Encode.string x

let private encodeStrings xs =
  Array.map Encode.string xs

type Vg = {
  name: string;
  uuid: string;
  size: int;
  pvs_major_minor: string [];
}

type Lv = {
  name: string;
  uuid: string;
  size: int;
  block_device: string;
}

type MdRaid = {
  path: Path;
  block_device: string;
  drives: Path [];
}

type MpathNodes = {
  major_minor: string;
  parent: string option;
  serial_83: string;
  serial_80: string;
  path: string;
  size: int;
}

type Mpath = {
  name: string;
  block_device: string;
  nodes: MpathNodes [];
}

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
      |> (Decode.required "name" Decode.string)
      |> (Decode.required "path" Decode.string)
      |> (Decode.required "block_device" Decode.string)
      |> (Decode.required "uuid" Decode.string)
      |> (Decode.required "size" Decode.string)
      |> (Decode.required "drives" (Decode.array Decode.string))

  let decoder =
    Decode.decodeString decode
      >> Result.mapError exn

type LegacyBlockDev = {
  major_minor: string;
  path: Path;
  paths: Path [];
  serial_80: string option;
  serial_83: string option;
  size: int;
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
        ("size", Encode.int size);
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


type LegacyDev =
  | LegacyBlockDev of LegacyBlockDev
  | LegacyZFSDev of LegacyZFSDev

type LegacyDevs = {
  devs: Map<string, LegacyDev>;
  lvs: Map<string, Map<string, Lv>>;
  vgs: Map<string, Vg>;
  mds: Map<string, MdRaid>;
  zfspools: Map<string, LegacyZFSDev>;
  zfsdatasets: Map<string, LegacyZFSDev>;
  local_fs: Map<string, (string * string)>;
}
