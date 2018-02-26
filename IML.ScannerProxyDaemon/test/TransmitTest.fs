// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.TransmitTest

open Fable.Core

open TestFixtures
open Fable.Import.Jest
open Matchers

open CommonLibrary
open Transmit

testList "Send Message" [
  let withSetup f ():unit =
    f();

  yield! testFixture withSetup [
    "Should return serialised Data message on incoming update", fun () ->
      let mutable mock = id
      mock <- jest.fn1()
      let testSend = sendMessage mock

      updateJson
        |> Data
        |> testSend
        |> ignore
      expect.Invoke(mock).toBeCalledWith(JsInterop.toJson (Data updateJson))

    "Should return serialised Heartbeat message on incoming heartbeat", fun () ->
      let mutable mock = id
      mock <- jest.fn1()
      let testSend = sendMessage mock

      Heartbeat
        |> testSend
        |> ignore
      expect.Invoke(mock).toBeCalledWith(JsInterop.toJson Heartbeat)
  ]
]
