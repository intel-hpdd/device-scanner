// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Devtree

open Fable
open Fable.Import.Node
open Elmish
open IML.DeviceAggregatorDaemon.Query
open IML.Types.ScannerStateTypes

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
