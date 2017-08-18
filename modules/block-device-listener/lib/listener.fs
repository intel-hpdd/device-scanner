// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module BlockDeviceListener.Listener

open Fable.Core
open Fable.Import.JS
open Fable.Import.Node
open UdevEventTypes.EventTypes

let private getRecordType (x:IAction) =
  match x.ACTION with
    | Actions.Add -> Add (x :?> IAdd)
    | Actions.Remove -> Remove (x :?> IRemove)

let private toJson = getRecordType >> JsInterop.toJson

[<Pojo>]
type NetPath = {
  path: string
}

let run (net:Net.IExports) (env:IAction) =
  let client = net.connect { path = "/var/run/device-scanner.sock"; }
  client.once(
    "connect",
    fun () -> client.``end``(toJson env)
  ) |> ignore
