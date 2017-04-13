module BlockDeviceListener.Test

open Jest
open Fable.Import.JS
open Fable.Core
open BlockDeviceListener.Listener
open UdevEventTypes.EventTypes

[<Pojo>]
type MockSocket = {
   once: (obj -> obj);
   ``end``: (obj -> unit);
}

[<Pojo>]
type MockNet = {
  connect: (NetPath -> MockSocket)
}

let createMocks () =
  let mockSocket = {
    once = jest.fn();
    ``end`` = jest.fn();
  }

  let mockNet = {
      connect = jest.fn(fun () -> mockSocket);
  }

  (mockSocket, mockNet)

test "should call connect with NetPath" <| fun () ->
  let (_, mockNet) = createMocks()

  let action = JsInterop.createEmpty<IAction>
  action.ACTION <- Actions.Add

  run (unbox mockNet) action

  toBeCalledWith mockNet.connect { path = "/var/run/device-scanner.sock"; }

test "should call connect" <| fun () ->
  let (mockSocket, mockNet) = createMocks()

  let action = JsInterop.createEmpty<IAction>
  action.ACTION <- Actions.Add

  run (unbox mockNet) action

  toBeCalledWith2 mockSocket.once "connect" (expect.any Function)

test "should call end with process data" <| fun () ->
  let (mockSocket, mockNet) = createMocks()

  let action = JsInterop.createEmpty<IAction>
  action.ACTION <- Actions.Add

  run (unbox mockNet) action

  mockSocket.once
  |> getMock
  |> fun x -> x.calls
  |> List.last<string * (unit -> unit)>
  |> snd
  |> fun fn -> fn()

  toBeCalledWith mockSocket.``end`` """{"Add":{"ACTION":"add"}}"""
