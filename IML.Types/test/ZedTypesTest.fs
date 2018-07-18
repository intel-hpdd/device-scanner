// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.ZedTypesTest

open Fable.Import.Jest
open Thoth.Json

open IML.Types.ZedTypes
open IML.CommonLibrary

open Matchers
open IML.Types.Fixtures
open Fable.PowerPack

test "decode / encode pools" <| fun () ->
  fixtures.pools
    |> Decode.decodeString Zed.decoder
    |> Result.unwrap
    |> Zed.encoder
    |> Encode.encode 2
    |> toMatchSnapshot
