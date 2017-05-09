module LineDelimitedJsonStream.Test

open Jest
open LineDelimitedJsonStream.Stream
open Fable.Import
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.Import.Node.Stream

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
let streamToPromise (a:Transform<string, 'a>) : Fable.Import.JS.Promise<'a[]> = jsNative

testDone "should handle errors from the stream" <| fun d ->
  expect.assertions(1)

  let jsonStream = getJsonStream<TestRec>()
  let err = JS.Error.Create "Unexpected end of JSON input"

  jsonStream
    .on("data", d.fail)
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
