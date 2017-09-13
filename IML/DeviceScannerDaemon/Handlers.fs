// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec IML.DeviceScannerDaemon.Handlers

open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.Import.Node
open System.Collections.Generic

open IML.DeviceScannerDaemon.ParseUdevDB
open IML.DeviceScannerDaemon.EventTypes

let private deviceMap = Dictionary<string, AddEvent>()

let handleReadEvent' getUdevDB ``end`` =
  promise {
    let! result = getUdevDB()

    deviceMap.Clear()

    parser result
      |> Array.map(extractAddEvent >> fun x -> deviceMap.Add (x.DEVPATH, x))
      |> ignore

    ``end`` None
  }

let handleReadEvent = handleReadEvent' getUdevDB

let dataHandler' (``end``:string option -> unit) = function
  | ReadEventMatch(_) ->
    handleReadEvent ``end``
      |> Promise.start
  | InfoEventMatch(_) ->
    ``end`` (Some (toJson deviceMap))
  | AddEventMatch(x) ->
    deviceMap.Add (x.DEVPATH, x)
    ``end`` None
  | RemoveEventMatch(x) ->
    deviceMap.Remove x.DEVPATH |> ignore
    ``end`` None
  | _ ->
    ``end`` None
    raise (System.Exception "Handler got a bad match")

let dataHandler = dataHandler'
