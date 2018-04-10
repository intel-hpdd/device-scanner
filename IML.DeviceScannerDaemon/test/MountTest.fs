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
open IML.Types.MountTypes
open IML.DeviceScannerDaemon.Mount

let matcher localMounts x =
  x
    |> update localMounts
    |> toMatchSnapshot

let private localMounts = Set.empty

let private decodeToTuple (s:string) =
  let m =
    s
      |> LocalMount.decoder
      |> Result.unwrap

  (Mount.MountPoint m.target,
   Mount.BdevPath m.source,
   Mount.FsType m.fstype,
   Mount.MountOpts m.opts)

let private snap (x:Result<LocalMounts, exn>) =
  x
    |> Result.unwrap
    |> LocalMounts.encoder
    |> Encode.encode 2
    |> toMatchSnapshot

let newMounts =
  (MountCommand.AddMount (fixtures.mount |> decodeToTuple))
    |> update localMounts
    |> Result.unwrap

test "Adding a new mount" <| fun () ->
  expect.assertions 1

  (MountCommand.AddMount (fixtures.mount |> decodeToTuple))
    |> update localMounts
    |> snap

test "Removing a mount" <| fun () ->
  expect.assertions 1

  (MountCommand.RemoveMount (fixtures.mount |> decodeToTuple))
    |> update newMounts
    |> snap

test "Remounting a mount with different options" <| fun () ->
  expect.assertions 1

  let newMounts =
    (MountCommand.AddMount (fixtures.mount |> decodeToTuple))
      |> update localMounts
      |> Result.unwrap

  let mountTuple = (fixtures.mount |> decodeToTuple)
  (MountCommand.ReplaceMount
    |> update newMounts
    |> snap

test "Removing a mount" <| fun () ->
  expect.assertions 1

  let newMounts =
    (MountCommand.AddMount (fixtures.mount |> decodeToTuple))
      |> update localMounts
      |> Result.unwrap

  (MountCommand.RemoveMount (fixtures.mount |> decodeToTuple))
    |> update newMounts
    |> snap
