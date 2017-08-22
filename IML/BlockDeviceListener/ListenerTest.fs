// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.BlockDeviceListener.ListenerTest

open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open Fable.Import.JS
open Fable.Core.JsInterop
open Fable.Core
open Fable.Import.Node
open Fable.Import.Node.Globals

describe "Listener" <| fun () ->
  let mutable mockOnce = null
  let mutable mockEnd = null
  let mutable mockConnect = null

  beforeEach <| fun () ->
    mockOnce <- Matcher2<string, obj -> obj, obj>()
    mockOnce?id <- 3
    mockEnd <- Matcher<string, unit>()

    mockConnect <- Matcher<obj, obj>(fun (_) ->
      createObj [
        "once" ==> mockOnce.Mock;
        "end" ==> mockEnd.Mock;
      ]
    )

    jest.mock("net", fun () -> createObj ["connect" ==> mockConnect.Mock])

    require.Invoke "./Listener.fs" |> ignore

  test "should call connect with NetPath" <| fun () ->
    mockConnect.CalledWith(createObj ["path" ==> "/var/run/device-scanner.sock"])

  test "should call connect" <| fun () ->
    mockOnce.CalledWith "connect" (expect.any Function)

  test "should call end with process data" <| fun () ->
    mockOnce.LastCall
      |> snd
      |> fun fn -> fn()
      |> ignore


    mockEnd.CalledWith <| expect.any String
