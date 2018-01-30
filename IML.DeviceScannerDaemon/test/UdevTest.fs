// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.DeviceScannerDaemon.UdevTest

open TestFixtures
open IML.Types.CommandTypes
open Udev
open Fable.Import.Jest
open Matchers


let matcher m x =
  x
    |> update m
    |> Map.toList
    |> toMatchSnapshot

test "Matching Events" <| fun () ->
  expect.assertions 6

  matcher Map.empty addUdev

  matcher Map.empty addDiskUdev

  matcher Map.empty addDmUdev

  matcher Map.empty removeUdev

  matcher Map.empty addMdraidUdev

  matcher Map.empty (UdevCommand.Change """{ "ACTION": "blah" }""")
