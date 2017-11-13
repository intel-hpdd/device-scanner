// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.LineDelimitedJsonStream.StreamTest

open Fable.Import.Jest
open Matchers
open Stream
open Fable.Import
open Fable.Core
open JsInterop
open Fable.PowerPack
open Fable.Import.Node

type TestRec = {
  foo:string;
  bar:string;
}

type TestRec2 = {
  baz:string;
}

type Test1or2 =
  | TestRec of TestRec
  | TestRec2 of TestRec2


[<Import("default", "stream-to-promise")>]
let streamToPromise (_:Stream.Transform<string, 'a>) : JS.Promise<'a[]> = jsNative

testDone "should handle errors from the stream" <| fun d ->
  expect.assertions 1

  let jsonStream = getJsonStream()
  let err = JS.Error.Create "Unexpected end of JSON input"

  let fail x =
    d.fail x
    |> ignore

  jsonStream
    .on("data", fail)
    .on("error", (toEqual err))
    .on("end", d.``done``) |> ignore

  jsonStream.write("""{ "food": "bard", """) |> ignore
  jsonStream.write("\n") |> ignore
  jsonStream.``end``()

testAsync "should handle empty JSON obj" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.``end``("{}");

  promise {
    let! res = streamToPromise jsonStream

    res == [| Json.Object([||]) |]
  }

testAsync "should handle string" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.``end``("\"Info\"");

  promise {
    let! res = streamToPromise jsonStream

    res == [| Json.String("Info") |]
  }

testAsync "should handle JSON in a single chunk" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.``end``("{ \"foo\": \"bar\", \"bar\": \"baz\" }\n");

  promise {
    let! res = streamToPromise jsonStream

    res == [|
      Json.Object([| ("foo", Json.String "bar"); ("bar", Json.String "baz") |])
    |]
  }

testAsync "should handle chunks of JSON" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.write("{ \"foo\": \"bar\", ") |> ignore
  jsonStream.``end``("\"bar\": \"baz\" }\n")

  promise {
    let! res = streamToPromise jsonStream

    res == [|
      Json.Object([| ("foo", Json.String "bar"); ("bar", Json.String "baz") |])
    |]
  }

testAsync "should handle newlines in a string" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.``end``(toJson({ foo = "bar\n"; bar = "baz" }) + "\n")

  promise {
    let! res = streamToPromise jsonStream

    res == [| Json.Object([| ("foo", Json.String "bar\n"); ("bar", Json.String "baz") |]) |]
  }

testAsync "should handle the final json line without a newline" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.``end``(toJson({ foo = "bar"; bar = "baz"; }))

  promise {
    let! res = streamToPromise jsonStream

    res == [| Json.Object([| ("foo", Json.String "bar"); ("bar", Json.String "baz") |]) |]
  }

testAsync "should handle multiple records correctly" <| fun () ->
  let jsonStream = getJsonStream()
  jsonStream.write("""{"TestRec": { "foo": "bar", """) |> ignore
  jsonStream.write("\"bar\": \"baz\" }}\n") |> ignore
  jsonStream.``end``("""{"TestRec2": {"baz": "bap"}}""")

  promise {
    let! res = streamToPromise jsonStream

    let exp = [|
      Json.Object [|
        ("TestRec", (Json.Object [| ("foo", Json.String "bar"); ("bar", Json.String "baz") |]) );
      |];
      Json.Object [|
        ("TestRec2", (Json.Object [| ("baz", Json.String "bap") |]) );
      |]
    |]

    res == exp
  }
