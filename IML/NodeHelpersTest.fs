// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.NodeHelpersTest

open Fable.Import.Jest
open Matchers
open Fable.Import.JS
open Fable.Core.JsInterop
open Fable.Import.Node
open Globals

testList "NodeHelpers" [
  let withSetup f () =
    let onFn evt (fn:string -> obj) = fn evt
    let mockOnAndOnce = Matcher2<string, string -> obj, obj>(onFn)
    let mockConnect = Matcher<obj, obj>()
    let mockFn = Matcher<string, obj>(fun evt -> createObj ["name" ==> evt])

    jest.mock("net", fun () -> createObj ["connect" ==> mockConnect.Mock])

    let nodeHelpers = require.Invoke "./NodeHelpers.fs"
    f(nodeHelpers, mockConnect, mockOnAndOnce, mockFn)

  yield! testFixture withSetup [
    "should expose onceConnect", fun (nodeHelpers, _, mockOnce, mockFn) ->
      let c = createObj ["once" ==> mockOnce.Mock]
      nodeHelpers?NetHelpers?onceConnect (id, c) |> ignore
      mockOnce.CalledWith "connect" (expect.any Function)

    "should expose onConnect", fun (nodeHelpers, _, mockOn, mockFn) ->
      let c = createObj ["on" ==> mockOn.Mock]
      nodeHelpers?NetHelpers?onConnect (id, c) |> ignore
      mockOn.CalledWith "connect" (expect.any Function)

    "should expose onData", fun (nodeHelpers, _, mockOn, mockFn) ->
      let c = createObj ["on" ==> mockOn.Mock]
      nodeHelpers?NetHelpers?onData (id, c) |> ignore
      mockOn.CalledWith "data" (expect.any Function)

    "should expose connect with NetPath", fun (nodeHelpers, mockConnect, _, _) ->
      nodeHelpers?NetHelpers?connect (createObj ["path" ==> "/var/run/device-scanner.sock" ]) |> ignore
      mockConnect.CalledWith (createObj ["path" ==> "/var/run/device-scanner.sock"])


    // "should call connect", fun (_, mockOnce, _) ->
    //   mockOnce.CalledWith "connect" (expect.any Function);

    // "should call end with process data", fun (_, mockOnce, mockEnd) ->
    //   mockOnce.LastCall
    //     |> snd
    //     |> fun fn -> fn()
    //     |> ignore

    //   mockEnd.CalledWith <| expect.any String
  ]
]
