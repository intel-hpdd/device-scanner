// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.JsonDecoders

open Thot.Json.Decode
open Fable.Import.Node.PowerPack.LineDelimitedJsonStream

let decodeJson (decoder: Decoder<'T>) =
    function
      | Json y -> decodeValue decoder y

let andThenSucceed x y = andThen (x >> succeed) y
