// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.BlockDeviceListener.Listener

open Fable.Core
open Fable.Import.JS
open Fable.Import.Node

[<Pojo>]
type NetPath = {
  path: string
}

let private client = Net.connect { path = "/var/run/device-scanner.sock"; }

client.once(
  "connect",
  fun () -> client.``end``(JSON.stringify Globals.``process``.env)
) |> ignore
