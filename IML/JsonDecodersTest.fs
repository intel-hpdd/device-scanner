module IML.JsonDecodersTest

open Thot.Json.Decode
open Fable.Import
open Fable.Import.Jest
open Matchers
open IML.JsonDecoders
open Fable.Import.Node.PowerPack.LineDelimitedJsonStream

let toJson =
  JS.JSON.parse
    >> Json

test "decodeJson" <| fun () ->
  (decodeJson string (toJson @"""foo""")) == Ok("foo")

test "andThenSucceed" <| fun () ->
  let decoder =
    field "bar" string
      |> andThenSucceed (fun x -> x + "baz")

  JS.JSON.parse """{"bar": "foo"}""" |> decoder == Ok("foobaz")
