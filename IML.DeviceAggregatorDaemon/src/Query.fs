// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Query

open LegacyParser
open IML.Types.LegacyTypes
open IML.Types.ScannerStateTypes
open Thoth.Json
open Fable.Import.Node.Http

type QueryType =
   | Legacy
   | Host of string

let matchPaths (hPaths : string list) (pPaths : string list) =
    pPaths
    |> List.filter (fun x -> List.contains x hPaths)
    |> (=) pPaths

let discoverZpools devTree (host : string) (ps : Map<string, LegacyZFSDev>)
  (ds : Map<string, LegacyZFSDev>) (blockDevices : LegacyBlockDev list) =
    let mutable pools = ps
    let mutable datasets = ds

    devTree
    // remove current host, we are looking for pools on other hosts
    |> Map.filter (fun k _ -> k <> host)
    |> Map.map (fun _ v ->
           // we want pools/datasets but don't need key
           let (pps, dds) =
               v.zed
               |> Map.toList
               |> List.map snd
               // keep pools if we have all their drives
               |> List.filter (fun p ->
                      let hostPaths =
                          blockDevices |> List.map (fun x -> (string x.path))
                      p.vdev
                      |> getDisks
                      |> matchPaths hostPaths)
               |> List.filter
                      (fun p ->
                      not (List.contains p.state [ "EXPORTED"; "UNAVAIL" ]))
               |> parsePools blockDevices
           pps |> Map.iter (fun k v -> pools <- Map.add k v pools)
           dds |> Map.iter (fun k v -> datasets <- Map.add k v datasets))
    |> ignore

    (pools, datasets)

let parseSysBlock devTree (host : string) (state : State) =
    let xs =
        state.blockDevices
        |> Map.toList
        |> List.map snd
        |> List.filter filterDevice
        |> List.map (LegacyBlockDev.ofUEvent state.blockDevices)

    let blockDeviceNodes : Map<string, LegacyBlockDev> =
        xs
        |> List.map (fun x -> (x.major_minor, x))
        |> Map.ofList

    let mutable blockDeviceNodes' =
        Map.map (fun _ v -> LegacyDev.LegacyBlockDev v) blockDeviceNodes

    let mpaths = Mpath.ofBlockDevices state.blockDevices

    let ndt =
        blockDeviceNodes
        |> NormalizedDeviceTable.create
        |> Mpath.addToNdt mpaths

    let vgs, lvs = parseDmDevs xs
    let mds = parseMdraidDevs xs ndt
    let zfspools, zfsdatasets = parseZfs xs state.zed
    let localFs = parseLocalFs state.blockDevices zfsdatasets state.localMounts
    let zfspools, zfsdatasets = discoverZpools devTree host zfspools zfsdatasets xs

    // update blockDeviceNodes map with zfs pools and datasets
    Map(Seq.concat [ (Map.toSeq zfspools)
                     (Map.toSeq zfsdatasets) ])
    |> Map.iter
           (fun _ v ->
           blockDeviceNodes' <- Map.add v.block_device
                                    (LegacyDev.LegacyZFSDev v) blockDeviceNodes')

    { // @TODO: need encoder for all below types
      devs = blockDeviceNodes'
      lvs = lvs
      vgs = vgs
      mds = mds
      local_fs = localFs
      zfspools = zfspools
      zfsdatasets = zfsdatasets
      mpath = mpaths }

let runQuery (response : ServerResponse) devTree queryType =
    match queryType with
    | Legacy ->
        devTree
        |> Map.map (fun k v ->
               parseSysBlock devTree k v
               |> LegacyDevTree.encode)
        |> Encode.dict
        |> Encode.encode 0
        |> response.``end``
    | _ ->
        eprintf "unsupported query type"
        response.``end`` ()
