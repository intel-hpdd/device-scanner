// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.LegacyParser

open IML.Types.UeventTypes
open IML.CommonLibrary
open IML.Types.LegacyTypes
open libzfs.Libzfs
open Fable

let filterDevice (x : UEvent) =
    x.size <> Some "0" && x.size <> None && x.readOnly <> Some true
    && not x.biosBoot

let createVgAndLv x =
    let vg =
        { name = x.dm_vg |> Option.expect ""
          uuid = x.vg_uuid |> Option.expect "Expected a vgUuid"
          size = x.dm_vg_size |> Option.expect "Expected a vg size"
          pvs_major_minor = x.dm_slave_mms }

    let lv =
        { name = x.dm_lv |> Option.expect "dm_lv field not found"
          uuid = x.lv_uuid |> Option.expect "Expected a lvUuid"
          size = x.size
          block_device = x.major_minor }

    vg, lv

let parseDmDevs xs =
    let out = (Map.empty, Map.empty)
    xs
    |> List.filter (fun x -> x.lv_uuid <> None)
    |> List.filter (fun x -> x.vg_uuid <> None)
    |> List.filter (fun x -> x.dm_lv <> None)
    |> List.map createVgAndLv
    |> (List.fold (fun (vgs, lvs) (vg, lv) ->
            let nestedMap =
                Map.tryFind vg.name lvs
                |> Option.defaultValue Map.empty
                |> Map.add lv.name lv
            ((Map.add vg.name vg vgs), (Map.add vg.name nestedMap lvs))) out)

let parseMdraidDevs xs ndt =
    xs
    |> List.filter (fun x -> x.md_uuid <> None)
    |> List.fold (fun m x ->
           let md =
               { path = x.path
                 block_device = x.major_minor
                 drives =
                     x.md_device_paths
                     |> Array.map Path
                     |> Array.map (NormalizedDeviceTable.normalizedDevicePath ndt)
                     |> Array.map (fun x ->
                            (List.find (fun y -> Array.contains x y.paths)
                                 xs).major_minor
                      )
                }
           Map.add (Option.get x.md_uuid) md m) Map.empty

let bdevOrLustreZfs (mount : IML.Types.MountTypes.LocalMount)
    (datasets : Map<string, LegacyZFSDev>) opt =
    match opt with
    | Some x -> Some(UEvent.majorMinor x, mount.target, mount.fstype)
    | None ->
        match mount.fstype with
        | "lustre" ->
            datasets
            |> Map.toList
            |> List.map snd
            |> List.filter (fun x -> x.name = mount.source)
            |> List.tryHead
            |> Option.map
                   (fun x -> (x.block_device, mount.target, mount.fstype))
        | _ -> None

let parseLocalFs blockDevices (datasets : Map<string, LegacyZFSDev>)
    (mounts : IML.Types.MountTypes.LocalMounts) =
    mounts
    |> Set.toList
    |> List.choose
           (fun x ->
           BlockDevices.tryFindByPath blockDevices (Path x.source)
           |> bdevOrLustreZfs x datasets)
    |> List.fold (fun m (mm, t, f) -> Map.add mm (t, f) m) Map.empty

let rec getDisks (vdev : VDev) =
    let collectChildDisks x =
        x
        |> List.ofArray
        |> List.collect getDisks
    match vdev with
    | Disk { Disk = disk } ->
        [ disk.path ]
    | File _ -> []
    | RaidZ { RaidZ = { children = xs } } | Mirror { Mirror = { children = xs } } | Replacing { Replacing = { children = xs } } ->
        collectChildDisks xs
    | Root { Root = { children = children; spares = spares; cache = cache } } ->
        [ children; spares; cache ] |> List.collect collectChildDisks

let parsePools (blockDevices : BlockDevices) (ps : Pool list) =
  ps
    |> List.fold (fun (ps, ds) p ->
      let mms =
          p.vdev
          |> getDisks
          |> List.map Path
          |> List.choose (BlockDevices.tryFindByPath blockDevices)
          |> List.map (UEvent.majorMinor)
          |> List.toArray

      let ds' : Map<string, LegacyZFSDev> =
        Array.fold (fun acc (d : Dataset) ->
          Map.add d.guid { name = d.name
                           path = d.name
                           block_device = sprintf "zfsset:%s" d.guid
                           uuid = d.guid
                           size =
                            (Array.find (fun (p : ZProp) ->
                                        p.name = "available") d.props).value
                           drives = mms } acc) ds p.datasets

      let pools =
          if Array.isEmpty p.datasets then
              let pool : LegacyZFSDev =
                  { name = p.name
                    path = p.name
                    block_device = sprintf "zfspool:%s" p.guid
                    uuid = p.guid
                    size = p.size
                    drives = mms }
              Map.add p.guid pool ps
          else ps

      (pools, ds')) (Map.empty, Map.empty)

let parseZfs (blockDevices : BlockDevices) (zed : IML.Types.ZedTypes.Zed) =
    zed
    |> Map.toList
    |> List.map snd
    |> parsePools blockDevices
