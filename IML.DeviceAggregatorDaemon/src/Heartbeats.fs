// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Heartbeats

open System

let heartbeatTimeout = 30000
let mutable heartbeats:Map<string,Timers.Timer> = Map.empty

let addHeartbeat handler host =
  match Map.tryFind host heartbeats with
  | Some x ->
    x.Stop()
    x.Start()
  | None ->
    let timer = new Timers.Timer(float heartbeatTimeout)
    timer.AutoReset <- false

    let cHandler = handler host
    timer.Elapsed.Add cHandler

    heartbeats <- Map.add host timer heartbeats

    Async.StartImmediate (async { timer.Start() })
