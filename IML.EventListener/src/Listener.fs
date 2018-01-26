// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.EventListener.Listener

open Fable.Import.JS
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open Fable.Import.Node.PowerPack.Stream

type NetPath = {
  path: string
}

let private client = net.connect { path = "/var/run/device-scanner.sock"; }

let private endClient d = Writable.``end`` d client

let private data:Buffer.Buffer option =
  Globals.``process``.env
    |> JSON.stringify
    |> buffer.Buffer.from
    |> Some

client
  |> onceConnect (fun () -> endClient data)
  |> ignore
