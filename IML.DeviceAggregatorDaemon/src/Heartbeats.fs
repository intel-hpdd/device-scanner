// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Heartbeats

open Fable.Import
open IML.CommonLibrary
let heartbeatTimeout = 30000
let mutable heartbeats:Map<string, JS.SetTimeoutToken> = Map.empty

let addHeartbeat handler host =
  Map.tryFind host heartbeats
    |> Option.map JS.clearTimeout
    |> ignore

  let onTimeout () =
    let token = Map.find host heartbeats
    JS.clearTimeout token
    heartbeats <- Map.remove host heartbeats
    (handler host)

  let token = JS.setTimeout onTimeout heartbeatTimeout

  heartbeats <- Map.add host token heartbeats
