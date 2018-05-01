// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Handlers

open Fable.Core.JsInterop
open Fable.Import.Node
open Fable.Import.Node.PowerPack

open IML.CommonLibrary

open IML.Types.MessageTypes
open IML.Types.ScannerStateTypes
open IML.Types.LegacyTypes
open LegacyParser
open Heartbeats
open Thoth.Json

let mutable devTree:Map<string, State> = Map.empty

let timeoutHandler host =
  printfn "Aggregator received no heartbeat from host %A" host
  devTree <- Map.remove host devTree

let matchPaths (hPaths:string list) (pPaths:string list) =
  pPaths
    |> List.filter (fun x -> List.contains x hPaths)
    |> (=) pPaths

let discoverZpools
    (host:string)
    (ps:Map<string,LegacyZFSDev>)
    (ds:Map<string,LegacyZFSDev>)
    (blockDevices:LegacyBlockDev list) =

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
                 blockDevices
                 |> List.map (fun x -> (string x.path))

               p.vdev
               |> getDisks
               |> matchPaths hostPaths
          )
          |> List.filter (fun p ->
               not (List.contains p.state ["EXPORTED"; "UNAVAIL"])
          )
          |> parsePools blockDevices
      pps
        |> Map.iter (fun k v -> pools <- Map.add k v pools)
      dds
        |> Map.iter (fun k v -> datasets <- Map.add k v datasets)
    )
    |> ignore
//   |> Map.iter (fun h (ps, ds) -> printf "pools: %A , datasets: %A discovered on host %s" ps ds h)
  (pools, datasets)

let parseSysBlock (host:string) (state:State) =
  let xs =
    state.blockDevices
      |> Map.toList
      |> List.map snd
      |> List.filter filterDevice
      |> List.map LegacyBlockDev.ofUEvent

  let blockDeviceNodes : Map<string,LegacyBlockDev> =
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

  let localFs = parseLocalFs state.blockDevices state.localMounts

  let zfspools, zfsdatasets = parseZfs xs state.zed

  let zfspools, zfsdatasets = discoverZpools host zfspools zfsdatasets xs

  // update blockDeviceNodes map with zfs pools and datasets
  Map(Seq.concat [ (Map.toSeq zfspools) ; (Map.toSeq zfsdatasets) ])
    |> Map.iter (fun _ v ->
         blockDeviceNodes' <- Map.add v.block_device (LegacyDev.LegacyZFSDev v) blockDeviceNodes'
       )

  // @TODO: need encoder for all below types
  {
    devs = blockDeviceNodes';
    lvs = lvs;
    vgs = vgs;
    mds = mds;
    local_fs = localFs;
    zfspools = zfspools;
    zfsdatasets = zfsdatasets;
    mpath = mpaths;
  }

let updateTree host x =
  let state =
    Decode.decodeString State.decoder x
      |> Result.unwrap

  Map.add host state devTree

let serverHandler (request:Http.IncomingMessage) (response:Http.ServerResponse) =
  match request.method with
    | Some "GET" ->
      devTree
        |> Map.map (fun k v -> parseSysBlock k v |> LegacyDevTree.encoder)
        |> toJson
        |> buffer.Buffer.from
        |> response.``end``
    | Some "POST" ->
      request
        |> Stream.reduce "" (fun acc x -> Ok (acc + x.toString("utf-8")))
        |> Stream.iter (fun x ->
            match !!request.headers?("x-ssl-client-name") with
              | Some "" ->
                eprintfn "Aggregator received message but hostname was empty"
              | Some host ->
                match Message.decoder x with
                  | Ok Message.Heartbeat ->
                    addHeartbeat timeoutHandler host
                  | Ok (Message.Data y) ->
                    printfn "Aggregator received update with devices from host %s" host
                    devTree <- updateTree host y
                  | Error x ->
                     eprintfn "Aggregator received message but message decoding failed (%A)" x
              | None ->
                eprintfn "Aggregator received message but x-ssl-client-name header was missing from request"

            response.``end``()
        )
        |> ignore
    | x ->
      response.``end``()
      eprintfn "Aggregator handler got a bad match %A" x
