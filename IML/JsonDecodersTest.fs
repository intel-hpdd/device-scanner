module IML.JsonDecodersTest

open Thot.Json.Decode
open Fable.Import
open Fable.Import.Jest
open Matchers
open IML.JsonDecoders
open Fable.Import.Node.PowerPack.Stream

let toJson =
  JS.JSON.parse
    >> LineDelimitedJson.Json

test "decodeJson" <| fun () ->
  (decodeJson string (toJson @"""foo""")) == Ok("foo")