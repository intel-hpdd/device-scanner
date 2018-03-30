// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.MountTest

open TestFixtures
open Mount
open Fable.Import.Jest
open Matchers

let matcher localMounts x =
  x
    |> update localMounts
    // |> Result.map Set.toList
    |> toMatchSnapshot

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
