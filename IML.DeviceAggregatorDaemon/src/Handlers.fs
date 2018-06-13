// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Handlers

open Fable
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open Thoth.Json
open Elmish
open IML.DeviceAggregatorDaemon.Query
open IML.Types.MessageTypes
open IML.Types.ScannerStateTypes
open IML.CommonLibrary

module Heartbeat =
    let heartbeatTimeout = 30000

    type Model =
        { heartbeats : Map<string, bool> }

    let init () =
        { heartbeats = Map.empty }, Cmd.none

    type Msg =
        | AddHeartbeat of string
        | RemoveHeartbeat of string

    let update msg model =
        match msg with
        | AddHeartbeat x ->
            { model with heartbeats = Map.add x true model.heartbeats }, Cmd.none
        | RemoveHeartbeat x ->
            { model with heartbeats = Map.remove x model.heartbeats }, Cmd.none

    let expireAndReset model =
        let heartbeats =
            model.heartbeats
            |> Map.filter (fun _ v -> v)
            |> Map.map (fun _ _ -> false)
        { model with heartbeats = heartbeats }

module Devtree =
    type Model =
        { tree : Map<string, State> }

    let init () =
        { tree = Map.empty }, Cmd.none

    type Msg =
        | GetTree of Http.ServerResponse
        | UpdateTree of string * State

    let update msg (model:Model) =
        match msg with
        | GetTree response ->
            runQuery response model.tree Legacy
            model, Cmd.none
        | UpdateTree ((host), (state)) ->
            { model with tree = Map.add host state model.tree }, Cmd.none

    let expire model notExpired =
        let tree =
            model.tree
            |> Map.filter (fun k _ -> Map.containsKey k notExpired)
        { model with tree = tree }

module App =
    type Model =
        { heartbeats : Heartbeat.Model
          tree : Devtree.Model }

    type Msg =
        | Tick
        | Heartbeats of Heartbeat.Msg
        | Devtree of Devtree.Msg

    let expire model =
        printfn "expiring and resetting heartbeats"
        let notExpired =
            Heartbeat.expireAndReset model.heartbeats

        { heartbeats = notExpired
          tree = Devtree.expire model.tree notExpired.heartbeats },
          Cmd.none

    let timer initial =
        let sub (dispatch:Msg -> unit) =
            JS.setInterval
                (fun _ -> dispatch Tick) Heartbeat.heartbeatTimeout
                |> ignore
        Cmd.ofSub sub

    let init () =
        let heartbeats, heartbeatCmd = Heartbeat.init ()
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
            let res, cmd = Heartbeat.update msg' model.heartbeats
            { model with heartbeats = res }, Cmd.map Heartbeats cmd
        | Devtree msg' ->
            let res, cmd = Devtree.update msg' model.tree
            { model with tree = res }, Cmd.map Devtree cmd

    let serverHandler (request : Http.IncomingMessage) (response : Http.ServerResponse) : Msg option =
        match request.method with
        | Some "GET" ->
            Msg.Devtree (Devtree.GetTree response)
            |> Some
        | Some "POST" ->
            let mutable command = None

            request
            |> Stream.reduce "" (fun acc x -> Ok(acc + x.toString ("utf-8")))
            |> Stream.iter (fun x ->
                 match !!request.headers?("x-ssl-client-name") with
                 | Some "" ->
                     eprintfn "Aggregator received message but hostname was empty"
                 | Some host ->
                     match Message.decoder x with
                     | Ok Message.Heartbeat ->
                         command <- Msg.Heartbeats (Heartbeat.AddHeartbeat host) |> Some
                     | Ok(Message.Data y) ->
                         let state =
                             y
                             |> Decode.decodeString State.decoder
                             |> Result.unwrap
                         command <- Msg.Devtree (Devtree.UpdateTree (host, state)) |> Some
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

            command
        | x ->
            eprintfn "Aggregator handler got a bad match %A" x
            None

    let handler initial =
        let sub (dispatch:Msg -> unit) =
            let handler' (request : Http.IncomingMessage) (response : Http.ServerResponse) =
                serverHandler request response
                |> Option.map dispatch
                |> ignore

            let server = http.createServer handler'
            let fd = createEmpty<Net.Fd>

            fd.fd <- 3
            server.listen (fd) |> ignore

        Cmd.ofSub sub

    Program.mkProgram init update (fun model _ -> printf "%A\n" model)
    |> Program.withSubscription timer
    |> Program.withSubscription handler
    |> Program.run
