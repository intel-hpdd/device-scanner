// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.LineDelimitedJsonStream.StreamTest

open Fable.Import.Jest
open Matchers
open Util
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

  jsonStream.write(Buffer.Buffer.from """{ "food": "bard", """) |> ignore
  jsonStream.write(Buffer.Buffer.from "\n") |> ignore
  jsonStream.``end``()

testAsync "should handle empty JSON obj" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.``end``(Buffer.Buffer.from "{}");

  promise {
    let! res = streamToPromise jsonStream

    res == [ Json.Object([||]) ]
  }

testAsync "should handle string" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.``end``(Buffer.Buffer.from "\"Info\"");

  promise {
    let! res = streamToPromise jsonStream

    res == [ Json.String("Info") ]
  }

testAsync "should handle JSON in a single chunk" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.``end``(Buffer.Buffer.from "{ \"foo\": \"bar\", \"bar\": \"baz\" }\n");

  promise {
    let! res = streamToPromise jsonStream

    res == [
      Json.Object([| ("foo", Json.String "bar"); ("bar", Json.String "baz") |])
    ]
  }

testAsync "should handle chunks of JSON" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.write(Buffer.Buffer.from "{ \"foo\": \"bar\", ") |> ignore
  jsonStream.``end``(Buffer.Buffer.from "\"bar\": \"baz\" }\n")

  promise {
    let! res = streamToPromise jsonStream

    res == [
      Json.Object([| ("foo", Json.String "bar"); ("bar", Json.String "baz") |])
    ]
  }

testAsync "should handle newlines in a string" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.``end``(Buffer.Buffer.from (toJson({ foo = "bar\n"; bar = "baz" }) + "\n"))

  promise {
    let! res = streamToPromise jsonStream

    res == [ Json.Object([| ("foo", Json.String "bar\n"); ("bar", Json.String "baz") |]) ]
  }

testAsync "should handle the final json line without a newline" <| fun () ->
  let jsonStream = getJsonStream()

  jsonStream.``end``(Buffer.Buffer.from  (toJson({ foo = "bar"; bar = "baz"; })))

  promise {
    let! res = streamToPromise jsonStream

    res == [ Json.Object([| ("foo", Json.String "bar"); ("bar", Json.String "baz") |]) ]
  }

testAsync "should handle multiple records correctly" <| fun () ->
  let jsonStream = getJsonStream()
  jsonStream.write(Buffer.Buffer.from """{"TestRec": { "foo": "bar", """) |> ignore
  jsonStream.write(Buffer.Buffer.from "\"bar\": \"baz\" }}\n") |> ignore
  jsonStream.``end``(Buffer.Buffer.from """{"TestRec2": {"baz": "bap"}}""")

  promise {
    let! res = streamToPromise jsonStream

    let exp = [
      Json.Object [|
        ("TestRec", (Json.Object [| ("foo", Json.String "bar"); ("bar", Json.String "baz") |]) );
      |];
      Json.Object [|
        ("TestRec2", (Json.Object [| ("baz", Json.String "bap") |]) );
      |]
    ]

    res == exp
  }
