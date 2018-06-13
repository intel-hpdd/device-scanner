// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Handlers

//open Fable.Core.JsInterop
open Fable.Import.Node
//open Fable.Import.Node.PowerPack
//open IML.CommonLibrary
//open IML.Types.MessageTypes
//open IML.Types.ScannerStateTypes
//open Thoth.Json
//open Query
open Fable
open Fable.Import

open Elmish

module Heartbeat =
    let heartbeatTimeout = 30000

    type Model =
        { heartbeats : Map<string, int> }

    let init hosts =
        let heartbeats =
            hosts
            |> Array.map (fun x -> x, 0)
        { heartbeats = Map.ofArray heartbeats }, Cmd.none

    type Msg =
        | AddHeartbeat of string
        | RemoveHeartbeat of string

    let update msg model =
        match msg with
        | AddHeartbeat x ->
            { model with heartbeats = Map.add x 1 model.heartbeats }, Cmd.none
        | RemoveHeartbeat x ->
            { model with heartbeats = Map.remove x model.heartbeats }, Cmd.none

    let expireAndReset model =
        let heartbeats =
            model.heartbeats
            |> Map.filter (fun _ v -> v > 0)
            |> Map.map (fun _ _ -> 0)
        { model with heartbeats = heartbeats }

module Devtree =
    type Model =
        { tree : Map<string, string> }

    let init hosts =
        let pairs =
            hosts
            |> Array.map (fun x -> x, "")
        { tree = Map.ofArray pairs }, Cmd.none

    type Msg =
        | GetTree
        | UpdateTree of string * string

    let update msg model =
        match msg with
        | GetTree ->
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
        // | Reset
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

    let init hosts =
        let heartbeats, heartbeatCmd = Heartbeat.init hosts
        let tree, treeCmd = Devtree.init hosts

        { heartbeats = heartbeats
          tree = tree },
        Cmd.batch [ Cmd.map Heartbeats heartbeatCmd
                    Cmd.map Devtree treeCmd ]

    let update msg model : Model * Cmd<Msg> =
        match msg with
        | Tick ->
            expire model
        // | Reset ->
            // init Array.empty
        | Heartbeats msg' ->
            let res, cmd = Heartbeat.update msg' model.heartbeats
            { model with heartbeats = res }, Cmd.map Heartbeats cmd
        | Devtree msg' ->
            let res, cmd = Devtree.update msg' model.tree
            { model with tree = res }, Cmd.map Devtree cmd

    //let rec handleTree
    //  (state : DevTree) (command : AggregatorCommand) : DevTree =
    //    match command with
    //    | GetTree response ->
    //          runQuery response state Legacy
    //          state
    //    | UpdateTree ((host), (data)) ->
    //          data
    //          |> Decode.decodeString State.decoder
    //          |> Result.unwrap
    //          |> (fun x -> Map.add host x state)
    //    | _ ->
    //          state
    //
    let serverHandler (request : Http.IncomingMessage) (response : Http.ServerResponse) =
        match request.method with
        | Some "GET" ->
            Cmd.ofMsg Devtree.GetTree
            // treeReducer (GetTree response) |> ignore
        // | Some "POST" ->
            // request
            // |> Stream.reduce "" (fun acc x -> Ok(acc + x.toString ("utf-8")))
            // |> Stream.iter (fun x ->
                  //  match !!request.headers?("x-ssl-client-name") with
                  //  | Some "" ->
                        //  eprintfn "Aggregator received message but hostname was empty"
                  //  | Some host ->
                        //  match Message.decoder x with
                        //  | Ok Message.Heartbeat ->
                              //  heartbeatReducer (AddHeartbeat (host, heartbeatReducer)) |> ignore
                        //  | Ok(Message.Data y) ->
                              //  treeReducer (UpdateTree (host, y)) |> ignore
                        //  | Error x ->
                              //  eprintfn
                                  //  "Aggregator received message but message decoding failed (%A)"
                                  //  x
                  //  | None ->
                        //  eprintfn
                            // "Aggregator received message but x-ssl-client-name header was missing from request"
            // )
            |> ignore
        | x ->
            eprintfn "Aggregator handler got a bad match %A" x
        response.``end``()

    // let handler initial =
        // let sub (dispatch:Msg -> unit) =
            // serverHandler request response
        // Cmd.ofSub sub

    Program.mkProgram init update (fun model _ -> printf "%A\n" model)
    |> Program.withSubscription timer
    // |> Program.withSubscription handler
    |> Program.run
