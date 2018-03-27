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
    f(Matcher())

  yield! testFixture withSetup [
    "Should return serialised Data message on incoming update", fun m ->
      updateJson
        |> Data
        |> sendMessage m.Mock
        |> ignore

      m <?> JsInterop.toJson (Data updateJson);
    "Should return serialised Heartbeat message on incoming heartbeat", fun m ->
      Heartbeat
        |> sendMessage m.Mock
        |> ignore

      m <?> JsInterop.toJson Heartbeat;
  ]
]
