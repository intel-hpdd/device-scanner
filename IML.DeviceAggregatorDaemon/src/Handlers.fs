// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Handlers

open Fable.Core.JsInterop
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open IML.Types.MessageTypes

open Heartbeats
open Fable

let mutable devTree:Map<string,string> = Map.empty

let timeoutHandler host =
  printfn "Aggregator received no heartbeat from host %A" host
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
        |> Stream.reduce "" (fun acc x -> Ok (acc + x.toString("utf-8")))
        |> Stream.iter (fun x ->
            match !!request.headers?("x-ssl-client-name") with
              | Some "" ->
                eprintfn "Aggregator received message but hostname was empty"
              | Some host ->
                match (Message.decoder x) with
                  | Ok Heartbeat ->
                    addHeartbeat timeoutHandler host
                  | Ok (Data y) ->
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
