module IML.MountEmitterTest

open Fable.Import.Jest
open Matchers

open Fable.Core.JsInterop

describe "mocking external" <| fun () ->
  let mutable CommonLibrary: obj = null
  let mutable mockMatcher: Matcher<string, float> = null

  beforeEach <| fun () ->
    mockMatcher <- Matcher<string, float>()

    jest.mock("commonLibrary", fun () ->
      createObj ["sendData" ==> mockMatcher.Mock]
    )

    CommonLibrary <- Fable.Import.Node.Globals.require.Invoke "commonLibrary"


  test "should work with mocking external deps" <| fun () ->
    open IML.MountEmitter
    Globals.``process``.send "hello\n"

    toBe "foo" mockMatcher.LastCall
