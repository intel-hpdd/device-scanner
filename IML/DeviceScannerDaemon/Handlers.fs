// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open Fable.Core.JsInterop
open Fable.PowerPack
open System.Collections.Generic

open EventTypes
open IML.JsonDecoders

let private deviceMap = Dictionary<DevPath, AddEvent>()

let (|Info|_|) (x:Map<string,Json.Json>) =
  match x with
    | x when hasAction "info" x -> Some()
    | _ -> None

let dataHandler (``end``:string option -> unit) x =
  x
   |> unwrapObject
   |> function
      | Info ->
        ``end`` (Some (toJson deviceMap))
      | UdevAdd(x) | UdevChange(x) ->
        deviceMap.Add (x.DEVPATH, x)
        ``end`` None
      | UdevRemove(x) ->
        deviceMap.Remove x |> ignore
        ``end`` None
      | _ ->
        ``end`` None
        raise (System.Exception "Handler got a bad match")
