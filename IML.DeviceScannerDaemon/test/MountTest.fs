// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.MountTest

open TestFixtures
open Mount
open Fable.Import.Jest
open Matchers

let matcher x =
  x
    |> update Map.empty
    |> Result.map Map.toList
    |> toMatchSnapshot

test "Matching Events" <| fun () ->
  expect.assertions 4

  matcher addMount

  matcher unMount

  matcher reMount

  matcher moveMount
