// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.EventListener.ListenerTest

open Fable.Import.Jest
open Matchers
open Fable.Import.JS
open Fable.Core.JsInterop
open Fable.Import.Node
open Globals

testList "Listener" [
  let withSetup f () =
    let mockOnce = Matcher2<string, obj -> obj, obj>()
    let mockEnd = Matcher<string, unit>()

    let mockConnect = Matcher<obj, obj>(fun (_) ->
      createObj [
        "once" ==> mockOnce.Mock;
        "end" ==> mockEnd.Mock;
      ]
    )

    jest.mock("net", fun () -> createObj ["connect" ==> mockConnect.Mock])

    require.Invoke "./Listener.fs" |> ignore

    f(mockConnect, mockOnce, mockEnd)

  yield! testFixture withSetup [
    "should call connect with NetPath", fun (mockConnect, _, _) ->
      mockConnect.CalledWith(createObj ["path" ==> "/var/run/device-scanner.sock"]);

    "should call connect", fun (_, mockOnce, _) ->
      mockOnce.CalledWith "connect" (expect.any Function);

    "should call end with process data", fun (_, mockOnce, mockEnd) ->
      mockOnce.LastCall
        |> snd
        |> fun fn -> fn()
        |> ignore

      mockEnd.CalledWith <| expect.any String
  ]
]
