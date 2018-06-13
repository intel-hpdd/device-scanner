// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Heartbeats

open Elmish

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
