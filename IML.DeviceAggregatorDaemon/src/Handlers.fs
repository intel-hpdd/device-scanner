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

open LegacyParser
open Heartbeats


let mutable devTree:Map<string, State> = Map.empty

let timeoutHandler host =
  printfn "Aggregator received no heartbeat from host %A" host
  devTree <- Map.remove host devTree

let parseSysBlock (state:State) =
  let xs =
    state.blockDevices
      |> Map.toList
      |> List.map (snd >> LegacyBlockDev.createFromUEvent)
      |> List.filter LegacyBlockDev.filterDevice
      |> LegacyBlockDev.linkParents

  let blockDeviceNodes : Map<string,LegacyBlockDev> =
    xs
      |> List.map (fun x -> (x.major_minor, x))
      |> Map.ofList

  let ndt = NormalizedDeviceTable.create blockDeviceNodes

  let vgs, lvs = LegacyBlockDev.parseDmDevs xs

  let mds = LegacyBlockDev.parseMdraidDevs xs ndt

  let localFs = LegacyBlockDev.parseLocalFs state.blockDevices state.localMounts

  let zfspools, zfsdatasets = LegacyBlockDev.parseZfs state.zed xs


  // @TODO update blockDeviceNodes map with zfsPool, zfsdataset output.
  // @TODO aggregate zfs pairs between hosts

  {
    devs = blockDeviceNodes;
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
