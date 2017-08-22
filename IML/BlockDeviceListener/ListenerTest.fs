// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.BlockDeviceListener.ListenerTest

open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open Fable.Import.JS
open Fable.Core
open IML.BlockDeviceListener.Listener
open IML.UdevEventTypes.EventTypes

[<Pojo>]
type MockSocket = {
   once: string -> (obj -> obj) -> obj;
   ``end``: (string -> unit);
}

[<Pojo>]
type MockNet = {
  connect: (NetPath -> MockSocket)
}

let createMocks () =
  let mockOnce = Matcher2<string, obj -> obj, obj>()
  let mockEnd = Matcher<string, unit>()

  let mockSocket = {
    once = mockOnce.Mock;
    ``end`` = mockEnd.Mock;
  }

  let mockConnect = Matcher(fun (_) -> mockSocket)
  let mockNet = {
      connect = mockConnect.Mock;
  }

  (mockSocket, mockNet, mockConnect, mockOnce, mockEnd)

test "should call connect with NetPath" <| fun () ->
  let (_, mockNet, mockConnect, _, _) = createMocks()

  let action = JsInterop.createEmpty<IAction>
  action.ACTION <- Actions.Add

  run (unbox mockNet) action

  mockConnect.CalledWith { path = "/var/run/device-scanner.sock"; }

test "should call connect" <| fun () ->
  let (mockSocket, mockNet, _, mockOnce, _) = createMocks()

  let action = JsInterop.createEmpty<IAction>
  action.ACTION <- Actions.Add

  run (unbox mockNet) action

  mockOnce.LastCall ==  ("connect", (expect.any Function))

test "should call end with process data" <| fun () ->
  let (mockSocket, mockNet, _, mockOnce, mockEnd) = createMocks()

  let action = JsInterop.createEmpty<IAction>
  action.ACTION <- Actions.Add

  run (unbox mockNet) action

  mockOnce.LastCall
    |> snd
    |> fun fn -> fn()
    |> ignore

  mockEnd.CalledWith """{"Add":{"ACTION":"add"}}"""
