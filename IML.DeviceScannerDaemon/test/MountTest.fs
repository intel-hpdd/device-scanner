// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.MountTest

open Fable.Import.Jest
open Matchers
open Fixtures
open IML.CommonLibrary
open IML.Types.CommandTypes
open Fable.PowerPack
open Thot.Json
open IML.Types

let matcher localMounts x =
  x
    |> update localMounts
    // |> Result.map Set.toList
    |> toMatchSnapshot

let private localMounts = Set.empty

let private snap (x:Result<LocalMount, exn>) =
  x
    |> Result.unwrap
    |> UeventTypes.BlockDevices.encoder
    |> Encode.encode 2
    |> toMatchSnapshot

test "Adding a new blockdevice" <| fun () ->
  (UdevCommand.Add (fixtures.add))
    |> update blockDevices
    |> snap

test "Changing a blockdevice" <| fun () ->
  let blockDevices' =
    (UdevCommand.Add (fixtures.add))
      |> update blockDevices
      |> Result.unwrap

  (UdevCommand.Change (fixtures.change))
    |> update blockDevices'
    |> snap

test "Removing a blockdevice" <| fun () ->
  let blockDevices' =
    (UdevCommand.Add (fixtures.add))
      |> update blockDevices
      |> Result.unwrap

let private mounts =
  let mount =
    fixtures.pool
      |> Libzfs.Pool.decoder
      |> Result.unwrap

  Map.ofList [(pool.guid, pool)]

test "encoding pools" <| fun () ->
  pools
    |> Zed.encode
    |> Json.Encode.encode 2
    |> toMatchSnapshot

test "getPoolInState" <| fun () ->
  guid
   |> Zed.getPoolInState pools
   |> Result.isOk
   |> (===) true
test "Matching Events" <| fun () ->
  expect.assertions 4

  let matcher = matcher Set.empty

  matcher addMount

  matcher unMount

  matcher reMount

  matcher moveMount

test "Events on existing mount" <| fun () ->
  expect.assertions 2

  let mounts = addMount |> update Set.empty
  mounts
    |> Result.map(fun lmounts ->
      matcher lmounts addMount
    ) |> ignore
  mounts
    |> Result.map(fun lmounts ->
      matcher lmounts unMount
    ) |> ignore
