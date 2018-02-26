// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.HandlersTest

open Handlers
open TestFixtures
open Fable.Import.Jest
open Matchers
open IML.Types.CommandTypes

testList "Data Handler" [
  let withSetup f ():unit =
    f (backCompatHandler)

  yield! testFixture withSetup [
    "Should call end with map for info event", fun (handler) ->
      handler infoUdev
        |> toMatchSnapshot

    "Should add then remove a device path", fun (handler) ->
      expect.assertions 2
      handler (UdevCommand addUdev) |> toMatchSnapshot

      handler (UdevCommand removeUdev) |> toMatchSnapshot
    ]
]
