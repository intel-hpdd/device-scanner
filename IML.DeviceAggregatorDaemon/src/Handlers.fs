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
  /// Identify imported pools that reside on drives this host can see
  ///   - gather information on pools active on other hosts
  ///   - check the local host is reporting (can see) the underlying drives of said pool
  ///   - verify the poor we want to add hasn't also been reported as active one another host
  ///   - verify the localhost isn't also reporting the pool as active (it shouldn't be)
  ///   - add pool and contained datasets to those to be reported to be connected to local host
  /// :return: the new dictionary of devices reported on the given host
  devTree
    // remove current host, we are looking for pools on other hosts
    |> Map.filter (fun k _ -> k <> host)
    |> Map.map (fun _ v ->
      // we want pools/datasets but don't need key
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
    )
//   |> Map.iter (fun h (ps, ds) -> printf "pools: %A , datasets: %A discovered on host %s" ps ds h)

let parseSysBlock (state:State) =
  let xs =
    state.blockDevices
      |> Map.toList
      |> List.map (snd >> createFromUEvent)
      |> List.filter filterDevice
      |> linkParents

  let blockDeviceNodes : Map<string,LegacyBlockDev> =
    xs
      |> List.map (fun x -> (x.major_minor, x))
      |> Map.ofList

  let ndt = NormalizedDeviceTable.create blockDeviceNodes

  let blockDeviceNodes' =
    Map.map (fun _ v -> LegacyDev.LegacyBlockDev v) blockDeviceNodes

  let vgs, lvs = parseDmDevs xs

  let mds = parseMdraidDevs xs ndt

  let localFs = parseLocalFs state.blockDevices state.localMounts

  let zfspools, zfsdatasets = parseZfs xs state.zed

  // let zfspools, zfsdatasets = discoverZpools zfspools zfsdatasets xs

  // @TODO update blockDeviceNodes map with zfsPool, zfsdataset output, append because type should be DU Block or ZFS
  // @TODO aggregate zfs pairs between hosts

  // TODO: need encoder for all below types
  {
    devs = blockDeviceNodes';
    lvs = lvs;
    vgs = vgs;
    mds = mds;
    local_fs = localFs;
    zfspools = zfspools;
    zfsdatasets = zfsdatasets;
  }

let updateTree host x =
  let state =
    State.decoder x
      |> Result.unwrap

  Map.add host state devTree

let serverHandler (request:Http.IncomingMessage) (response:Http.ServerResponse) =
  match request.method with
    | Some "GET" ->
      devTree
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
