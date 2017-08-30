// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec IML.DeviceScannerDaemon.Handlers

open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.Import.Node
open System.Collections.Generic

open IML.DeviceScannerDaemon.ParseUdevDB
open IML.UdevEventTypes.EventTypes

let private deviceMap = Dictionary<string, AddEvent>()

let dataHandler (c:Net.Socket) = function
  | ReadEventMatch(_) ->
    promise {
      let! result = getUdevDB()

      deviceMap.Clear()

      parser result
        |> Array.map(extractAddEvent >> fun x -> deviceMap.Add (x.DEVPATH, x))
        |> ignore

      c.``end``()
    }
      |> Promise.start
  | InfoEventMatch(_) ->
    c.``end``(toJson deviceMap)
  | AddEventMatch(x) ->
    deviceMap.Add (x.DEVPATH, x)
    c.``end``()
  | RemoveEventMatch(x) ->
    deviceMap.Remove x.DEVPATH |> ignore
    c.``end``()
  | _ ->
    c.``end``()
    raise (System.Exception "Handler got a bad match")
