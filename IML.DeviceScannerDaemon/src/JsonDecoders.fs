// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.JsonDecoders

open Thot.Json.Decode
open Fable.Import.Node.PowerPack.Stream

let decodeJson (decoder: Decoder<'T>) =
    function
      | LineDelimitedJson.Json y -> decodeValue decoder y
