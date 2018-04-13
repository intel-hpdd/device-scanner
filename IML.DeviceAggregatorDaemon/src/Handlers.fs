// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Handlers

open Fable.Core.JsInterop
open Fable.Import.Node
open Fable.Import.Node.PowerPack.Stream
open IML.Types.MessageTypes

open Heartbeats
open Fable

let mutable devTree:Map<string,string> = Map.empty

let timeoutHandler host _ =
  printfn "Aggregator received no heartbeat from host %A after %A ms" host heartbeatTimeout
  let timer = Map.tryFind host heartbeats

  match timer with
  | Some x ->
    x.Stop()
    x.Close()
    heartbeats <- Map.remove host heartbeats
  | None ->
    eprintfn "Aggregator could not find entry for host %A when timeout occurred" host

  devTree <- Map.remove host devTree

let serverHandler (request:Http.IncomingMessage) (response:Http.ServerResponse) =
  match request.method with
  | Some "GET" ->
    devTree
      |> toJson
      |> buffer.Buffer.from
      |> response.``end``
  | Some "POST" ->
    request
      |> reduce "" (fun acc x -> Ok (acc + x.toString("utf-8")))
      |> iter (fun x ->
          let hostname:string option = !!request.headers?("x-ssl-client-name")

          match hostname with
          | Some host ->
            match host with
            | "" ->
              eprintfn "Aggregator received message but hostname was empty"
            | _ ->
              match (Message.decoder x) with
              | Ok y ->
                match y with
                | Heartbeat ->
                  addHeartbeat timeoutHandler host
                | Data y ->
                  printfn "Aggregator received update with devices from host %s" host
                  devTree <- Map.add host y devTree
              | Error y ->
                eprintfn "Aggregator received message but message decoding failed (%A)" y
          | None ->
            eprintfn "Aggregator received message but x-ssl-client-name header was missing from request"

          response.``end``()
      )
      |> ignore
  | x ->
    response.``end``()
    eprintfn "Aggregator handler got a bad match %A" x
