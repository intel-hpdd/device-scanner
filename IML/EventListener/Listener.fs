// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.EventListener.Listener

open Fable.Import.JS
open Fable.Import.Node
open NodeHelpers.NetHelpers

let private client = connect { path = "/var/run/device-scanner.sock"; }

let private endClient = ``end`` client

let private data:Buffer.Buffer option =
  Globals.``process``.env
    |> JSON.stringify
    |> Buffer.Buffer.from
    |> Some

client
  |> onceConnect (fun () -> endClient data)
  |> ignore
