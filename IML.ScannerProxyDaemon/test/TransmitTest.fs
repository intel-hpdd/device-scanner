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

let testTransmit = jest.fn1()
let testSend = sendMessage testTransmit

testList "Send Message" [
  Test("Should return serialised Data message on incoming update", fun () ->
    updateJson
      |> Data
      |> testSend
    expect.Invoke(testTransmit).toBeCalledWith(JsInterop.toJson (Data updateJson))
  )

  Test("Should return serialised Heartbeat message on incoming heartbeat", fun () ->
    Heartbeat
      |> testSend
    expect.Invoke(testTransmit).toBeCalledWith(JsInterop.toJson Heartbeat)
  )
]
