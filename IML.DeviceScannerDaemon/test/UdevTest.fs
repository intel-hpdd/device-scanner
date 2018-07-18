// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.UdevTest

open Udev
open Fable.Import.Jest
open Matchers
open IML.Types.Fixtures
open IML.CommonLibrary
open IML.Types.CommandTypes
open Fable.PowerPack
open Thoth.Json
open IML.Types
open IML.Types.UeventTypes

let private blockDevices = Map.empty

let private snap (x : Result<BlockDevices, exn>) =
    x
    |> Result.unwrap
    |> UeventTypes.BlockDevices.encoder
    |> Encode.encode 2
    |> toMatchSnapshot

test "Adding a new blockdevice" <| fun () ->
    (UdevCommand.Add(fixtures.add))
    |> update blockDevices
    |> snap
test "Changing a blockdevice" <| fun () ->
    let blockDevices' =
        (UdevCommand.Add(fixtures.add))
        |> update blockDevices
        |> Result.unwrap
    (UdevCommand.Change(fixtures.change))
    |> update blockDevices'
    |> snap
test "Removing a blockdevice" <| fun () ->
    let blockDevices' =
        (UdevCommand.Add(fixtures.add))
        |> update blockDevices
        |> Result.unwrap
    (UdevCommand.Remove(fixtures.remove))
    |> update blockDevices'
    |> Result.unwrap
    |> (==) Map.empty
