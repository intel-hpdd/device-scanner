// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.ZedTest

open Zed
open IML.CommonLibrary
open Thoth
open Fable.Import.Jest
open Matchers
open IML.Types.Fixtures
open Fable.PowerPack
open IML.Types.ZedTypes
open IML.Types.CommandTypes

let private guid = Zpool.Guid "0xcf0b97290edafa56"

let private pools =
    fixtures.pools
    |> Json.Decode.decodeString Zed.decoder
    |> Result.unwrap

test "getPoolInState" <| fun () ->
    guid
    |> Zed.getPoolInState pools
    |> Result.isOk
    |> (===) true
test "remove pool" <| fun () ->
    guid
    |> Zed.removePool pools
    |> (==) Map.empty
test "remove dataset" <| fun () ->
    let pool =
        pools
        |> Map.toList
        |> List.head
        |> snd
    pool
    |> Zed.removeDataset (Zfs.Name "test/ds2")
    |> toMatchSnapshot
test "update prop" <| fun () ->
    let xs = [||]
    Zed.updateProp "foo:bar" "baz" xs |> (==) [| { name = "foo:bar"
                                                   value = "baz" } |]
test "exporting pool updates the state" <| fun () ->
    Zed.update pools (ZedCommand.ExportZpool(guid, (Zpool.State "exported")))
    |> Result.unwrap
    |> (==) Map.empty
test "destroying pool updates the state" <| fun () ->
    Zed.update pools (ZedCommand.DestroyZpool guid)
    |> Result.unwrap
    |> (==) Map.empty
test "destroying zfs updates the state" <| fun () ->
    ZedCommand.DestroyZfs(guid, Zfs.Name "test/ds2")
    |> Zed.update pools
    |> Result.unwrap
    |> Zed.encoder
    |> Json.Encode.encode 2
    |> toMatchSnapshot
test "setting a new zpool prop" <| fun () ->
    ZedCommand.SetZpoolProp(guid, "foo:bar", "baz")
    |> Zed.update pools
    |> Result.unwrap
    |> Zed.encoder
    |> Json.Encode.encode 2
    |> toMatchSnapshot
test "setting a new zfs prop" <| fun () ->
    ZedCommand.SetZfsProp(guid, Zfs.Name "test/ds", "foo:bar", "baz")
    |> Zed.update pools
    |> Result.unwrap
    |> Zed.encoder
    |> Json.Encode.encode 2
    |> toMatchSnapshot
