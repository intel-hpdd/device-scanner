// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.MountTypes

open IML.CommonLibrary
open Thot.Json

type LocalMount =
  {
    target: string;
    source: string;
    fstype: string;
    opts: string;
  }

module LocalMount =
  let encode
    {
      target = target;
      source = source;
      fstype = fstype;
      opts = opts;
    } =
      Encode.object [
        ("target", Encode.string target);
        ("source", Encode.string source);
        ("fstype", Encode.string fstype);
        ("opts", Encode.string opts);
      ]

  let decode =
    Decode.decode
      (fun target source fstype opts ->
        {
          target = target;
          source = source;
          fstype = fstype;
          opts = opts;
        }
      )
      |> (Decode.required "target" Decode.string)
      |> (Decode.required "source" Decode.string)
      |> (Decode.required "fstype" Decode.string)
      |> (Decode.required "opts" Decode.string)

  let decoder =
    Decode.decodeString decode
      >> Result.mapError exn


type LocalMounts = Set<LocalMount>

module LocalMounts =
  let encoder x =
    x
      |> Set.map LocalMount.encode
      |> Set.toArray
      |> Encode.array

  let decoder x =
    x
      |> Decode.array LocalMount.decode
      |> Result.map Set.ofArray
