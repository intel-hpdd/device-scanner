// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.App

open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open Thoth.Json
open Elmish
open IML.Types.MessageTypes
open IML.Types.ScannerStateTypes
open IML.CommonLibrary


type Model =
    { heartbeats : Heartbeats.Model
      tree : Devtree.Model }

type Msg =
    | Tick
    | Heartbeats of Heartbeats.Msg
    | Devtree of Devtree.Msg

let expire model =
    printf "expiring and resetting heartbeats"
    let notExpired =
        Heartbeats.expireAndReset model.heartbeats

    { heartbeats = notExpired
      tree = Devtree.expire model.tree notExpired.heartbeats },
      Cmd.none

let timer _ =
    let sub (dispatch:Msg -> unit) =
        JS.setInterval
            (fun _ -> dispatch Tick) Heartbeats.heartbeatTimeout
            |> ignore
    Cmd.ofSub sub

let init () =
    let heartbeats, heartbeatCmd = Heartbeats.init ()
    let tree, treeCmd = Devtree.init ()

    { heartbeats = heartbeats
      tree = tree },
    Cmd.batch [ Cmd.map Heartbeats heartbeatCmd
                Cmd.map Devtree treeCmd ]

let update msg model : Model * Cmd<Msg> =
    match msg with
    | Tick ->
        expire model
    | Heartbeats msg' ->
        let res, cmd = Heartbeats.update msg' model.heartbeats
        { model with heartbeats = res }, Cmd.map Heartbeats cmd
    | Devtree msg' ->
        let res, cmd = Devtree.update msg' model.tree
        { model with tree = res }, Cmd.map Devtree cmd

let serverHandler
    (request : Http.IncomingMessage) (response : Http.ServerResponse) dispatch =
    match request.method with
    | Some "GET" ->
        response
        |> Devtree.GetTree
        |> Msg.Devtree
        |> dispatch
    | Some "POST" ->
        request
        |> Stream.reduce "" (fun acc x -> Ok(acc + x.toString ("utf-8")))
        |> Stream.iter (fun x ->
             match !!request.headers?("x-ssl-client-name") with
             | Some "" ->
                 eprintfn "Aggregator received message but hostname was empty"
             | Some host ->
                 match Message.decoder x with
                 | Ok Message.Heartbeat ->
                     host
                     |> Heartbeats.AddHeartbeat
                     |> Msg.Heartbeats
                     |> dispatch
                 | Ok (Message.Data y) ->
                     let state =
                         y
                         |> Decode.decodeString State.decoder
                         |> Result.unwrap
                     (host, state)
                     |> Devtree.UpdateTree
                     |> Msg.Devtree
                     |> dispatch
                 | Error x ->
                     eprintfn
                         "Aggregator received message but message decoding failed (%A)"
                         x
             | None ->
                 eprintfn
                    "Aggregator received message but x-ssl-client-name header was missing from request"
        )
        |> ignore
        response.``end``()
    | x ->
        eprintfn "Aggregator handler got a bad match %A" x

let handler _ =
    let sub (dispatch:Msg -> unit) =
        let handler' (request : Http.IncomingMessage) (response : Http.ServerResponse) =
            serverHandler request response dispatch
            |> ignore

        let server = http.createServer handler'
        let fd = createEmpty<Net.Fd>

        fd.fd <- 3
        server.listen (fd) |> ignore

    Cmd.ofSub sub

Program.mkProgram init update (fun model _ -> printf "%A\n" model.heartbeats)
|> Program.withSubscription timer
|> Program.withSubscription handler
|> Program.withConsoleTrace
|> Program.run
