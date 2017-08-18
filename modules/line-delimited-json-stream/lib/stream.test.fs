// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module LineDelimitedJsonStream.Test

open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open LineDelimitedJsonStream.Stream
open Fable.Import
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.Import.Node

type TestRec = {
  bar:string;
  foo:string;
}

type TestRec2 = {
  baz:string;
}

type Test1or2 =
  | TestRec of TestRec
  | TestRec2 of TestRec2


[<Import("default", "stream-to-promise")>]
let streamToPromise (a:Stream.Transform<string, 'a>) : Fable.Import.JS.Promise<'a[]> = jsNative

testDone "should handle errors from the stream" <| fun d ->
  expect.assertions(1)

  let jsonStream = getJsonStream<TestRec>()
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

testAsync "should handle JSON in a single chunk" <| fun () ->
  let jsonStream = getJsonStream<TestRec>()

  jsonStream.``end``("{ \"foo\": \"bar\", \"bar\": \"baz\" }\n");

  promise {
    let! res = streamToPromise jsonStream

    toEqual res [|{ bar = "baz"; foo = "bar"; }|]
  }

testAsync "should handle chunks of JSON" <| fun () ->
  let jsonStream = getJsonStream<TestRec>()

  jsonStream.write("{ \"foo\": \"bar\", ") |> ignore
  jsonStream.``end``("\"bar\": \"baz\" }\n")

  promise {
    let! res = streamToPromise jsonStream

    toEqual res [|{ bar = "baz"; foo = "bar"; }|]
  }

testAsync "should handle newlines in a string" <| fun () ->
  let jsonStream = getJsonStream<TestRec>()

  jsonStream.``end``(toJson({ foo = "bar\n"; bar = "baz" }) + "\n")

  promise {
    let! res = streamToPromise jsonStream

    toEqual res [|{ bar = "baz"; foo = "bar\n"; }|]
  }

testAsync "should handle the final json line without a newline" <| fun () ->
  let jsonStream = getJsonStream<TestRec>()

  jsonStream.``end``(toJson({ foo = "bar"; bar = "baz"; }))

  promise {
    let! res = streamToPromise jsonStream

    toEqual res [|{ bar = "baz"; foo = "bar"; }|]
  }

testAsync "should handle multiple records correctly" <| fun () ->
  let jsonStream = getJsonStream<Test1or2>()
  jsonStream.write("""{"TestRec": { "foo": "bar", """) |> ignore
  jsonStream.write("\"bar\": \"baz\" }}\n") |> ignore
  jsonStream.``end``("""{"TestRec2": {"baz": "bap"}}""")

  promise {
    let! res = streamToPromise jsonStream

    let exp = [|
      TestRec { bar = "baz"; foo = "bar"; };
      TestRec2 { baz = "bap"; };
    |]

    toEqual res exp
  }
