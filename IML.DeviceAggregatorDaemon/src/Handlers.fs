// Copyright (c) 2018 DDN. All rights reserved.
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
open IML.Types.UeventTypes
open LegacyParser
open Heartbeats
open Thoth.Json

let mutable devTree : Map<string, State> = Map.empty

let timeoutHandler host =
    printfn "Aggregator received no heartbeat from host %A" host
    devTree <- Map.remove host devTree


/// The goal of this function is to find pools on other
/// hosts that are using some backing storage on
/// this host.
/// To do this, we need to find the device associated with the pool
/// and match it to any device on this host.
let discoverZpools (host : string) (blockDevices : BlockDevices) =
  let hostPaths =
    blockDevices
    |> Map.values
    |> List.choose UEvent.tryFindById
    |> Set.ofList

  let otherPools =
    devTree
    |> Map.filterKeys ((<>) host)
    |> Map.values
    |> List.collect (fun x ->
      x.zed
      |> Map.values
      |> List.filter (fun x -> x.state <> "EXPORTED")
      |> List.filter (fun x -> x.state <> "UNAVAIL")
      |> List.filter (fun x ->
        let poolPaths =
          x.vdev
          |> getDisks
          |> List.map Path
          |> List.choose (BlockDevices.tryFindByPath blockDevices)
          |> List.choose UEvent.tryFindById
          |> Set.ofList

        not (Set.isEmpty poolPaths) && Set.isSubset poolPaths hostPaths
      )
    )

  parsePools blockDevices otherPools

let parseSysBlock (host : string) (state : State) =
    let xs =
        state.blockDevices
        |> Map.values
        |> List.filter filterDevice
        |> List.map (LegacyBlockDev.ofUEvent state.blockDevices)

    let blockDeviceNodes : Map<string, LegacyBlockDev> =
        xs
        |> List.map (fun x -> (x.major_minor, x))
        |> Map.ofList

    let mpaths = Mpath.ofBlockDevices state.blockDevices

    let ndt =
      blockDeviceNodes
      |> NormalizedDeviceTable.create
      |> Mpath.addToNdt mpaths

    let vgs, lvs = parseDmDevs xs
    let mds = parseMdraidDevs xs ndt
    let zfspools, zfsdatasets = parseZfs state.blockDevices state.zed
    let localFs = parseLocalFs state.blockDevices zfsdatasets state.localMounts
    let zfspools', zfsdatasets' = discoverZpools host state.blockDevices


    let legacyBlockDeviceNodes =
        Map.mapValues LegacyDev.LegacyBlockDev blockDeviceNodes

    let legacyZfsNodes =
      zfspools'
      |> Map.merge zfsdatasets'
      |> Map.merge zfspools
      |> Map.merge zfsdatasets
      |> Map.mapAll (fun _ v ->
        (v.block_device, LegacyDev.LegacyZFSDev v)
      )

    {
      devs =  Map.merge legacyZfsNodes legacyBlockDeviceNodes
      lvs = lvs
      vgs = vgs
      mds = mds
      local_fs = localFs
      zfspools = Map.merge zfspools zfspools'
      zfsdatasets = Map.merge zfsdatasets zfsdatasets'
      mpath = mpaths }

let updateTree host x =
    let state = Decode.decodeString State.decoder x |> Result.unwrap
    Map.add host state devTree

let serverHandler (request : Http.IncomingMessage)
    (response : Http.ServerResponse) =
    match request.method with
    | Some "GET" ->
        devTree
        |> Map.map (fun k v -> parseSysBlock k v |> LegacyDevTree.encode)
        |> Encode.dict
        |> Encode.encode 0
        |> response.``end``
    | Some "POST" ->
        request
        |> Stream.reduce "" (fun acc x -> Ok(acc + x.toString ("utf-8")))
        |> Stream.iter (fun x ->
               match !!request.headers?("x-ssl-client-name") with
               | Some "" ->
                   eprintfn "Aggregator received message but hostname was empty"
               | Some host ->
                   match Message.decoder x with
                   | Ok Message.Heartbeat -> addHeartbeat timeoutHandler host
                   | Ok(Message.Data y) ->
                       printfn
                           "Aggregator received update with devices from host %s"
                           host
                       devTree <- updateTree host y
                   | Error x ->
                       eprintfn
                           "Aggregator received message but message decoding failed (%A)"
                           x
               | None ->
                   eprintfn
                       "Aggregator received message but x-ssl-client-name header was missing from request"
               response.``end``())
        |> ignore
    | x ->
        response.``end``()
        eprintfn "Aggregator handler got a bad match %A" x
