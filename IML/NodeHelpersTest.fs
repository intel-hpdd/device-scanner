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
    let onStringToObj evt (fn:string -> obj) = fn evt
    let mockOnStringToObj = Matcher<string, obj>()
    let mockOnAndOnce = Matcher2<string, string -> obj, obj>(onStringToObj)
    let mockConnect = Matcher<obj, obj>()
    let onObjToObj opts (fn:obj -> obj) = fn opts
    let mockOnObjToObj = Matcher<obj, obj>()
    let mockCreateServer = Matcher2<obj, obj -> obj, obj>(onObjToObj)

    jest.mock("net", fun () -> createObj [
                                "connect" ==> mockConnect.Mock
                                "createServer" ==> mockCreateServer.Mock
    ])

    let nodeHelpers = require.Invoke "./NodeHelpers.fs"
    f(nodeHelpers, mockConnect, mockCreateServer, mockOnAndOnce, mockOnStringToObj, mockOnObjToObj)

  yield! testFixture withSetup [
    "should expose onceConnect", fun (nodeHelpers, _, _, mockOnce, mockOnStringToObj, _) ->
      let c = createObj ["once" ==> mockOnce.Mock]
      nodeHelpers?NetHelpers?onceConnect (mockOnStringToObj.Mock, c) |> ignore
      mockOnce.CalledWith "connect" (expect.any Function)
      mockOnStringToObj.CalledWith "connect"

    "should expose onConnect", fun (nodeHelpers, _, _, mockOn, mockOnStringToObj, _) ->
      let c = createObj ["on" ==> mockOn.Mock]
      nodeHelpers?NetHelpers?onConnect (mockOnStringToObj.Mock, c) |> ignore
      mockOn.CalledWith "connect" (expect.any Function)
      mockOnStringToObj.CalledWith "connect"

    "should expose onData", fun (nodeHelpers, _, _, mockOn, mockOnStringToObj, _) ->
      let c = createObj ["on" ==> mockOn.Mock]
      nodeHelpers?NetHelpers?onData (mockOnStringToObj.Mock, c) |> ignore
      mockOn.CalledWith "data" (expect.any Function)
      mockOnStringToObj.CalledWith "data"

    "should expose onError", fun (nodeHelpers, _, _, mockOn, mockOnStringToObj, _) ->
      let c = createObj ["on" ==> mockOn.Mock]
      nodeHelpers?NetHelpers?onError (mockOnStringToObj.Mock, c) |> ignore
      mockOn.CalledWith "error" (expect.any Function)
      mockOnStringToObj.CalledWith "error"

    "should expose connect with NetPath", fun (nodeHelpers, mockConnect, _, _, _, _) ->
      nodeHelpers?NetHelpers?connect (createObj ["path" ==> "/var/run/device-scanner.sock" ]) |> ignore
      mockConnect.CalledWith (createObj ["path" ==> "/var/run/device-scanner.sock"])

    "should expose createServer", fun (nodeHelpers, _, mockCreateServer, _, _, mockOnObjToObj) ->
      let opts = createObj ["opt1" ==> "val1"]
      nodeHelpers?NetHelpers?createServer (opts, mockOnObjToObj.Mock) |> ignore
      mockCreateServer.CalledWith opts (expect.any Function)
      mockOnObjToObj.CalledWith opts
  ]
]
