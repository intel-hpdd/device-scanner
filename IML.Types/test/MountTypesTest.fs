// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.MountTypesTest

open Fable.Import.Jest
open Matchers
open Thoth.Json

open IML.CommonLibrary

open MountTypes
open Fixtures

test "decode / encode Mounts" <| fun () ->
  fixtures.mounts
    |> Decode.decodeString LocalMounts.decoder
    |> Result.unwrap
    |> LocalMounts.encoder
    |> Encode.encode 2
    |> toMatchSnapshot
