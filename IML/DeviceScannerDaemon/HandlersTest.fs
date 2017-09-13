module IML.DeviceScannerDaemon.HandlersTest

open IML.DeviceScannerDaemon.Handlers
open IML.DeviceScannerDaemon.TestFixtures
open Fable.Import.Node
open Fable.Core.JsInterop
open Fable.Core
open Fable.PowerPack
open Fable.Import
open Fable.Import.Jest
open Fable.Import.Jest.Matchers

let toJson =  Json.ofString >> Result.unwrapResult

testList "Read Handler" [
  let withSetup f () =
    let getUdevDB = Matcher<unit, JS.Promise<string>>(fun () ->
      Promise.lift ""
    )

    let ``end`` = Matcher<string option, unit>()

    promise {
      do! handleReadEvent' getUdevDB.Mock ``end``.Mock

      f (getUdevDB, ``end``)
    }

  yield! testFixtureAsync withSetup [
    "should call getUdevDB", fun (getUdevDB, ``end``) ->
      expect.Invoke(getUdevDB.Calls).toEqual([|[||]|])

    "should call end", fun (getUdevDB, ``end``) ->
      ``end`` <?> None
  ]
]

testList "Data Handler" [
  let withSetup f ():unit =
    let ``end`` = Matcher<string option, unit>()

    let handler = dataHandler' ``end``.Mock

    f (``end``, handler)

  yield! testFixture withSetup [
    "Should call end with map for info event", fun (``end``, handler) ->
      handler (toJson """{ "ACTION": "info" }""")
      ``end`` <?> Some("{}")

    "Should call end for add event", fun (``end``, handler) ->
       handler addObj
       ``end`` <?> None;

    "Should call end for remove event", fun (``end``, handler) ->
      handler removeObj
      ``end`` <?> None;

    "Should end on a bad match", fun (``end``, handler) ->
      expect.assertions 2
      try
        handler (toJson """{}""")
      with
        | ex ->
          ``end`` <?> None
          expect.Invoke(ex.Message).toEqual("Handler got a bad match")

  ]
]
