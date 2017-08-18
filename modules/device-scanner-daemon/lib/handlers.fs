// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec DeviceScannerDaemon.Handlers

open Fable.Core
open Fable
open Fable.Import.Node
open System.Collections.Generic
open UdevEventTypes.EventTypes

let deviceMap:IDictionary<string, IAdd> = dict[||]

let dataHandler (c:Net.Socket) = function
  | Info ->
    c.``end``(Fable.Core.JsInterop.toJson deviceMap)
  | Add(x) ->
    deviceMap.Add (x.DEVPATH, x)
    c.``end``()
  | Remove(x) ->
    deviceMap.Remove x.DEVPATH |> ignore
    c.``end``()
