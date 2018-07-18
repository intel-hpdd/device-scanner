// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.MountTest

open Fable.Import.Jest
open Matchers
open IML.CommonLibrary
open IML.Types.CommandTypes
open Fable.PowerPack
open Thoth.Json
open IML.Types.MountTypes
open IML.DeviceScannerDaemon.Mount

let private mountParamsShort =
    Mount.MountPoint "/", Mount.BdevPath "/foo/bar", Mount.FsType "ext4",
    Mount.MountOpts "rw,rela"

let private mountParamsReplace =
    let (target, source, fstype, opts) = mountParamsShort
    target, source, fstype, Mount.MountOpts "ro", opts

let private mountParamsMove =
    let (target, source, fstype, opts) = mountParamsShort
    Mount.MountPoint "/new", source, fstype, opts, target

let private snap (x : Result<LocalMounts, exn>) =
    x
    |> Result.unwrap
    |> LocalMounts.encoder
    |> Encode.encode 2
    |> toMatchSnapshot

let singleMount = (MountCommand.AddMount mountParamsShort) |> update Set.empty

test "Adding then removing a mount" <| fun () ->
    expect.assertions 2
    singleMount |> snap
    (MountCommand.RemoveMount mountParamsShort)
    |> update (singleMount |> Result.unwrap)
    |> snap
test "Remounting a mount with different options" <| fun () ->
    (MountCommand.ReplaceMount mountParamsReplace)
    |> update (singleMount |> Result.unwrap)
    |> snap
test "Moving a mount to a different mount-point" <| fun () ->
    (MountCommand.MoveMount mountParamsMove)
    |> update (singleMount |> Result.unwrap)
    |> snap
