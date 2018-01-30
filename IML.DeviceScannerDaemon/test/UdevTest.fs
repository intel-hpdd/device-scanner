// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.DeviceScannerDaemon.UdevTest

open TestFixtures
open Udev
open Fable.Import.Jest
open Matchers


let matcher x =
  x
    |> update Map.empty
    |> Map.toList
    |> toMatchSnapshot

test "Matching Events" <| fun () ->
  expect.assertions 5

  matcher addUdev

  matcher addDiskUdev

  matcher addDmUdev

  matcher removeUdev

  matcher addMdraidUdev
