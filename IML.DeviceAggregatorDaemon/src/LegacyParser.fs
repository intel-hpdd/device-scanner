// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec IML.DeviceAggregatorDaemon.LegacyParser

open System.Text.RegularExpressions

open Fable.Import.Node
open IML.Types.UeventTypes
open IML.CommonLibrary
open IML.Types.LegacyTypes
open libzfs.Libzfs

let private devPathRegex = "^/dev/[^/]+$"
let private diskByIdRegex = "^/dev/disk/by-id/"
let private diskByPathRegex = "^/dev/disk/by-path/"
let private mapperPathRegex = "^/dev/mapper/"

module NormalizedDeviceTable =
  let private filterByRegex r (Path(p)) =
    Regex.Match(p, r).Success

  let addNormalizedDevices xs ys m =
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
          Array.filter (filterByRegex devPathRegex) paths

        let diskByIdPaths =
          Array.filter (filterByRegex diskByIdRegex) paths

        let diskByPathPaths =
          Array.filter (filterByRegex diskByPathRegex) paths

        let mapperPaths =
          Array.filter (filterByRegex mapperPathRegex) paths

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
            let r = Array.filter (filterByRegex mapperPathRegex) x.paths

            v.dm_slave_mms
              |> List.ofArray
              |> List.map (fun x ->
                let dev =
                  Map.find x m

                let paths =
                  dev.paths
                    |> Array.filter (filterByRegex diskByIdRegex)

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


let private precedence = [|
    mapperPathRegex;
    diskByIdRegex;
    diskByPathRegex;
    ".+";
|]

let private idx (Path(x)) =
  Array.findIndex (fun p ->
    Regex.Match(x, p).Success
  ) precedence

let private sortPaths =
  Array.sortBy idx

let linkParents xs =
  let disks =
    xs
      |> List.filter (fun x -> x.device_type = "disk")

  xs
    |> List.map (fun x ->
      let (DevPath devPath) = x.device_path

      let parent = path.dirname devPath |> DevPath

      match List.tryFind (fun d -> d.device_path = parent) disks with
        | Some d ->
          {
             x with
              parent = Some d.major_minor
          }
        | None -> x
    )

let filterDevice (x:LegacyBlockDev) =
  x.size = 0 || x.is_ro = Some true

let parseLvmUUids (dmUuid:string option) =
  let lvmPfix = "LVM-"
  let uuidLen = 32

  let dmUuid' =
    dmUuid
      |> Option.expect "dmUuid was null"

  if dmUuid'.StartsWith(lvmPfix) |> not then
    failwithf "%s does not appear to be dmUuid" dmUuid'

  let uuids = dmUuid'.[lvmPfix.Length..]

  if uuids.Length <> (uuidLen * 2) then
    failwithf "%s does not have the expected length" dmUuid'

  (uuids.[0..(uuidLen - 1)], uuids.[uuidLen..])

let createVgAndLv x =
  let vgUuid, lvUuid = parseLvmUUids(x.dm_uuid)

  let vg = {
    name =
      x.dm_vg
        |> Option.expect "";
    uuid = vgUuid;
    size =
      x.dm_vg_size
        |> Option.expect ""
        |> (fun v -> v.Substring(0, v.Length - 1))
        |> int;
    pvs_major_minor = x.dm_slave_mms;
  }

  let lv = {
    name = x.dm_lv |> Option.expect "dm_lv field not found";
    uuid = lvUuid;
    size = x.size;
    block_device = x.major_minor
  }

  vg, lv

let parseDmDevs xs =
    let out = (Map.empty, Map.empty)

    xs
      |> List.filter (fun x -> x.dm_uuid <> None)
      |> List.filter (fun x -> x.dm_lv <> None)
      |> List.map createVgAndLv
      |> (List.fold
        (fun (vgs, lvs) (vg, lv) ->
          let nestedMap =
            Map.tryFind vg.name lvs
              |> Option.defaultValue Map.empty
              |> Map.add lv.name lv

          (
            (Map.add vg.name vg vgs),
            (Map.add vg.name nestedMap lvs)
          )
      ) out)

let parseMdraidDevs xs ndt =
  xs
    |> List.filter (fun x -> x.md_uuid <> None)
    |> List.fold
      (fun m x ->
        let md = {
          path = x.path;
          block_device = x.major_minor;
          drives =
            x.md_device_paths
              |> Array.map Path
              |> Array.map
                (NormalizedDeviceTable.normalizedDevicePath ndt)
              |> Array.map (fun x ->
                (List.find (fun (y:LegacyBlockDev) ->
                  x = y.path
                ) xs).path
              );
        }

        Map.add (Option.get x.md_uuid) md m
      ) Map.empty

let parseLocalFs blockDevices (mounts:IML.Types.MountTypes.LocalMounts) =
  mounts
    |> Set.toList
    |> List.choose (fun x ->
      BlockDevices.tryFindByPath blockDevices (Path x.source)
        |> Option.map (fun d -> (UEvent.majorMinor d, x.target, x.fstype))
    )
    |> List.fold (fun m (mm, t, f) ->
      Map.add mm (t, f) m
    ) Map.empty

let rec getDisks (vdev:VDev) =
  let collectChildDisks x =
    x
      |> List.ofArray
      |> List.collect getDisks


  match vdev with
    | Disk { Disk = disk } ->
      if disk.whole_disk = Some true then
        [ disk.path ]
      else
        []
    | File _ ->
      []
    | RaidZ { RaidZ = { children = xs } }
    | Mirror { Mirror = { children = xs } }
    | RaidZ { RaidZ = { children = xs } }
    | Replacing { Replacing = { children = xs } } ->
      collectChildDisks xs
    | Root { Root = { children = children; spares = spares; cache = cache } } ->
      [ children; spares; cache; ]
        |> List.collect collectChildDisks

let parseZfs (blockDevices:LegacyBlockDev list) (zed:IML.Types.ZedTypes.Zed) =
  zed
    |> Map.toList
    |> List.map snd
    |> parsePools blockDevices

let parsePools (blockDevices:LegacyBlockDev list) (ps:Pool list) =
  ps
    |> List.fold
      (fun (ps, ds) p ->
        let mms =
          p.vdev
            |> getDisks
            |> List.map (fun x ->
              let blockDev =
                List.find (fun y ->
                  Array.contains (Path x) y.paths
                ) blockDevices
              blockDev.major_minor
            )
            |> List.toArray

        let ds':Map<string, LegacyZFSDev> =
          Array.fold (fun acc (d:Dataset) ->
            Map.add
              d.guid
              {
                  name = d.name;
                  path = d.name;
                  block_device = sprintf "zfsset:%s" d.guid;
                  uuid = d.guid;
                  size = (Array.find (fun (p:ZProp) -> p.name = "available") d.props).value;
                  drives = mms;
              }
              acc
          ) ds p.datasets

        let pools =
          if Array.isEmpty p.datasets then
            let pool:LegacyZFSDev =
              {
                name = p.name;
                path = p.name;
                block_device = sprintf "zfspool:%s" p.guid;
                uuid = p.guid;
                size = p.size;
                drives = mms;
              }

            Map.add p.guid pool ps
          else
            ps

        (pools, ds')
      ) (Map.empty, Map.empty)

let createFromUEvent (x:UEvent) =
  let sorted = sortPaths x.paths

  let size =
    x.size
      |> Option.map int
      |> Option.defaultValue 0
      |> (*) 512

  {
    major_minor = UEvent.majorMinor x;
    path = Array.head sorted;
    paths = sorted;
    serial_80 = x.scsi80;
    serial_83 = x.scsi83;
    size = size;
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
