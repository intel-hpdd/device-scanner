module IML.JsonDecodersTest

open IML.JsonDecoders
open Fable.PowerPack
open Fable.Import.Jest
open Matchers

let private ofString = Json.ofString >> Result.unwrapResult
let private plainMap = ofString """{}"""

test "unwrap object" <| fun () ->
  expect.assertions(2)

  toMatchSnapshot (unwrapObject plainMap)

  expect.Invoke(fun () -> unwrapObject (ofString """null""")).toThrowErrorMatchingSnapshot()

test "object" <| fun () ->
  expect.assertions(2)

  toMatchSnapshot (object plainMap)
  toMatchSnapshot (object (ofString """null"""))

test "str" <| fun () ->
  expect.assertions(2)

  toMatchSnapshot (str (Json.String "foo"))
  toMatchSnapshot (str (ofString """null"""))

test "unwrap string" <| fun () ->
  expect.assertions(2)

  toMatchSnapshot (unwrapString (Json.String "bar"))

  expect.Invoke(fun () -> unwrapString (Json.Boolean true)).toThrowErrorMatchingSnapshot()
