// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec DeviceScannerDaemon.Handlers

open Fable.Core
open Fable
open Node.Net
open System.Collections.Generic
open UdevEventTypes.EventTypes

let deviceMap:IDictionary<string, IAdd> = dict[||]

let dataHandler (c:net_types.Socket) = function
  | Info -> c.write(Fable.Core.JsInterop.toJson deviceMap) |> ignore
  | Add(x) -> deviceMap.Add (x.DEVPATH, x)
  | Remove(x) -> deviceMap.Remove x.DEVPATH |> ignore
