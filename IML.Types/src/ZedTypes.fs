// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.ZedTypes

open libzfs
open Thoth.Json

type Zed = Map<string, Libzfs.Pool>

[<RequireQualifiedAccess>]
module Zed =
  let encoder pools =
    let zpoolValues =
      pools
        |> Map.toList
        |> List.map (fun (x, y) ->
          (x.ToString(), Libzfs.Pool.encode y)
        )

    Encode.object zpoolValues

  let decoder x =
    Decode.dict Libzfs.Pool.decode x
