// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.LegacyTypesTest

open Fable.Import.Jest
open Matchers
open Thot.Json

open IML.CommonLibrary

open LegacyTypes
open Fixtures
open IML.Types.LegacyTypes

test "decode / encode LegacyDev types" <| fun () ->
  fixtures.legacyZFSPool
    |> Decode.decodeString LegacyZFSDev.decode
    |> Result.unwrap
    |> LegacyZFSDev.encode
    // |> Encode.encode 2
    |> toMatchSnapshot
