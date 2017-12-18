// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.NodeHelpersTest

open Fable.Import.Jest
open Matchers
open Fable.Import.JS
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.Node
open Globals
open NodeHelpers
open System.Security.Cryptography.X509Certificates
open Fable.PowerPack.PromiseImpl
open Fable.Import
open Fable.AST.Fable.Util
open Fable.Core.Exceptions

testList "NetHelpers" [
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
    "should expose end", fun(nodeHelpers, _, _, _, _, _) ->
      let mockSocket = Matcher<string, unit>()
      let c = createObj ["end" ==> mockSocket.Mock]
      nodeHelpers?NetHelpers?``end`` (c, "some val") |> ignore
      mockSocket <?> "some val"

    "should expose onceConnect", fun (nodeHelpers, _, _, mockOnce, mockOnStringToObj, _) ->
      let c = createObj ["once" ==> mockOnce.Mock]
      nodeHelpers?NetHelpers?onceConnect (mockOnStringToObj.Mock, c) |> ignore
      mockOnce <??> ("connect", (expect.any Function))
      mockOnStringToObj <?> "connect"

    "should expose onConnect", fun (nodeHelpers, _, _, mockOn, mockOnStringToObj, _) ->
      let c = createObj ["on" ==> mockOn.Mock]
      nodeHelpers?NetHelpers?onConnect (mockOnStringToObj.Mock, c) |> ignore
      mockOn <??> ("connect", (expect.any Function))
      mockOnStringToObj <?> "connect"

    "should expose onData", fun (nodeHelpers, _, _, mockOn, mockOnStringToObj, _) ->
      let c = createObj ["on" ==> mockOn.Mock]
      nodeHelpers?NetHelpers?onData (mockOnStringToObj.Mock, c) |> ignore
      mockOn <??> ("data", (expect.any Function))
      mockOnStringToObj <?> "data"

    "should expose onError", fun (nodeHelpers, _, _, mockOn, mockOnStringToObj, _) ->
      let c = createObj ["on" ==> mockOn.Mock]
      nodeHelpers?NetHelpers?onError (mockOnStringToObj.Mock, c) |> ignore
      mockOn <??> ("error", (expect.any Function))
      mockOnStringToObj <?> "error"

    "should expose connect with NetPath", fun (nodeHelpers, mockConnect, _, _, _, _) ->
      nodeHelpers?NetHelpers?connect (createObj ["path" ==> "/var/run/device-scanner.sock" ]) |> ignore
      mockConnect <?> (createObj ["path" ==> "/var/run/device-scanner.sock"])

    "should expose createServer", fun (nodeHelpers, _, mockCreateServer, _, _, mockOnObjToObj) ->
      let opts = createObj ["opt1" ==> "val1"]
      nodeHelpers?NetHelpers?createServer (opts, mockOnObjToObj.Mock) |> ignore
      mockCreateServer <??> (opts, (expect.any Function))
      mockOnObjToObj <?> opts
  ]
]

testList "ChildProcessHelpers" [
  let withSetup f () =
    let execCallbackHandler cmd opts fn = fn (None, "bla", "bla2")
    let mockChildProcessExec = Matcher3<string, obj, ((obj option * string * string) -> unit), unit> (execCallbackHandler)
    let mockChildProcess = createObj ["exec" ==> mockChildProcessExec.Mock]
    jest.mock("child_process", fun () -> mockChildProcess)
    
    let nodeHelpers = require.Invoke "./NodeHelpers.fs"
    let p:JS.Promise<Result<(Stdout * Stderr),(ChildProcess.ExecError * Stdout * Stderr)>> = nodeHelpers?ChildProcessHelpers?exec("command") :?> JS.Promise<Result<(Stdout * Stderr),(ChildProcess.ExecError * Stdout * Stderr)>>

    f(p, mockChildProcessExec)

  yield! testFixtureAsync withSetup [
    "should call ChildProcess.exec", fun (p, mockExec) ->
      promise {
        let! x = p
        printfn "x: %A" x
        printfn "%A" mockExec.Calls
        mockExec <???> ("command", (expect.any Object), (expect.any Function))
      }
  ]
]
