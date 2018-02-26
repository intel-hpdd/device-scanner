// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.CommandTypesTest

open Fable.Import.Jest
open Matchers
open Fable.Core.JsInterop
open CommandTypes


test "v1 Action from JSON" <| fun () ->
  let x:Command = ofJson "{ \"ACTION\": \"info\" }"

  match x with
    | ACTION _ -> ()
    | x -> failwithf "expected ACTION.info, got %A" x


test "Info from JSON" <| fun () ->
  let x:Command = ofJson "\"Info\""

  match x with
    | Info -> ()
    | x -> failwithf "expected info, got %A" x

testList "ZedCommand" [
  let withSetup fn () =
    fn(function
      | ZedCommand x -> x
      | x -> failwithf "expected ZedCommand, got %A" x
    )


  yield! testFixture withSetup [
    "matches Init", fun fn ->
      "{\"ZedCommand\":\"Init\"}"
        |> ofJson
        |> fn
        |> function
          | Init -> ()
          | x -> failwithf "expected Init, got %A" x;
    "matches CreateZpool", fun fn ->
      "{\"ZedCommand\": {\"CreateZpool\":[\"foo\",\"3xa\",\"BLAH\"]}}"
        |> ofJson
        |> fn
        |> function
          | CreateZpool (n, g, s) ->
            n == Zpool.Name "foo"
            g == Zpool.Guid "3xa"
            s == Zpool.State "BLAH"
          | x -> failwithf "expected Init, got %A" x;
  ]
]


