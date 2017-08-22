// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec IML.DeviceScannerDaemon.Handlers

open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.Import.Node
open System.Collections.Generic
open IML.UdevEventTypes.EventTypes

let deviceMap:IDictionary<string, AddEvent> = dict[||]

let dataHandler (c:Net.Socket) = function
  | InfoEventMatch(_) ->
    c.``end``(toJson deviceMap)
  | AddEventMatch(x) ->
    deviceMap.Add (x.DEVPATH, x)
    c.``end``()
  | RemoveEventMatch(x) ->
    deviceMap.Remove x.DEVPATH |> ignore
    c.``end``()
  | _ ->
    raise (System.Exception "Handler Got a bad match")
