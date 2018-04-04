// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.MountTypesTest

open MountTypes

open Fable.Import.Jest
open Fable.Import
open Fable.Core.JsInterop
open IML.CommonLibrary
open Thot.Json
open Matchers
open Fixtures

[
  ("mount", fixtures.mount);
]
  |> List.map (fun ((name, x)) ->
    Test(name, fun () ->
      expect.assertions 2

      let decoded =
        x
        |> LocalMount.decoder
        |> Result.unwrap

      toMatchSnapshot decoded

      Set.ofList [
          decoded;
        ]
        |> LocalMounts.encoder
        |> Thot.Json.Encode.encode 2
        |> toMatchSnapshot
    )
  )
  |> testList "decode / encode UEvents "

// Reuse our snapshots to test decoding
let snaps:JS.Object = importAll "./__snapshots__/UeventTypesTest.fs.snap"

let ks =
  JS.Object.keys snaps
    |> Seq.filter (String.endsWith "2")
    |> Seq.map (fun k ->
      let entry:string =
        !!snaps?(k)
          |> String.filter (fun x -> x <> '\n')

      Test(k, fun () ->
        !!JS.JSON.parse(entry)
          |> Decode.decodeString (BlockDevices.decoder)
          |> Result.unwrap
          |> BlockDevices.encoder
          |> Thot.Json.Encode.encode 2
          |> toMatchSnapshot
      )
    )
    |> testList "decode encoded"
