// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.LegacyTypesTest

open Fable.Import.Jest
open Matchers
open Thoth.Json

open IML.CommonLibrary

open LegacyTypes
open Fixtures

test "decode / encode LegacyDev types" <| fun () ->
  fixtures.legacyZFSPool
    |> LegacyZFSDev.decoder
    |> Result.unwrap
    |> LegacyZFSDev.encode
    // |> Encode.encode 2
    |> toMatchSnapshot

  fixtures.legacyZFSDataset
    |> LegacyZFSDev.decoder
    |> Result.unwrap
    |> LegacyZFSDev.encode
    |> toMatchSnapshot

  fixtures.legacyBlockDev
    |> LegacyBlockDev.decoder
    |> Result.unwrap
    |> LegacyBlockDev.encode
    |> toMatchSnapshot
