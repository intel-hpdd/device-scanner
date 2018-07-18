// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.HeartbeatsTest

open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open Heartbeats
open Handlers

let private hostname = "foo.com"

testList "Heartbeat"
    [ let withSetup f () : unit =
          jest.useFakeTimers() |> ignore
          f()
          jest.clearAllTimers()
          heartbeats <- Map.empty
          jest.useRealTimers() |> ignore
      yield! testFixture withSetup
                 [ "should register timer",
                   fun () ->
                       heartbeats.ContainsKey hostname === false
                       addHeartbeat timeoutHandler hostname
                       heartbeats.ContainsKey hostname === true
                   "should expire timer on timeout",
                   fun () ->
                       addHeartbeat timeoutHandler hostname
                       heartbeats.ContainsKey hostname === true
                       jest.advanceTimersByTime (heartbeatTimeout + 100)
                       heartbeats.ContainsKey hostname === false
                   "should restart timer on next heartbeat",
                   fun () ->
                       addHeartbeat timeoutHandler hostname
                       jest.advanceTimersByTime (heartbeatTimeout - 100)
                       heartbeats.ContainsKey hostname === true
                       addHeartbeat timeoutHandler hostname
                       jest.advanceTimersByTime 200
                       heartbeats.ContainsKey hostname === true ] ]
