// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.HandlersTest

open Handlers
open IML.CommonLibrary
open Fable.Import.Jest
open Matchers
open IML.Types.CommandTypes
open IML.Types.ScannerStateTypes

test "stream returns the current state" <| fun () ->
  Command.Stream
    |> handler
    |> Result.unwrap
    |> (==) { blockDevices = Map.empty; zed = Map.empty; localMounts = Set.empty; }
