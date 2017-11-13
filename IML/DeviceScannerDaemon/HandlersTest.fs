module IML.DeviceScannerDaemon.HandlersTest

open IML.DeviceScannerDaemon.Handlers
open TestFixtures
open Fable.PowerPack
open Fable.Import.Jest
open Matchers


let private toJson =  Json.ofString >> Result.unwrapResult
let private mapToJson = Map.toArray >> Json.Object

let private addJson = mapToJson addObj
let private changeJson =
  addObj
    |> Map.add "ACTION" (Json.String "change")
    |> mapToJson
let private removeJson = mapToJson removeObj
let private infoJson = toJson """{ "ACTION": "info" }"""

testList "Data Handler" [
  let withSetup f ():unit =
    let ``end`` = Matcher<string option, unit>()

    let handler = dataHandler ``end``.Mock

    f (``end``, handler)

  yield! testFixture withSetup [
    "Should call end with map for info event", fun (``end``, handler) ->
      handler infoJson
      ``end`` <?> Some("{}");

    "Should call end for add event", fun (``end``, handler) ->
      handler addJson

      ``end`` <?> None;

    "Should call end for add event", fun (``end``, handler) ->
      handler changeJson

      ``end`` <?> None;

    "Should call end for remove event", fun (``end``, handler) ->
      handler removeJson

      ``end`` <?> None;

    "Should end on a bad match", fun (``end``, handler) ->
      expect.assertions 2

      expect.Invoke(fun () -> handler (toJson """{}""")).toThrowErrorMatchingSnapshot()

      ``end`` <?> None

    "Should remove an item", fun (``end``, handler) ->
        expect.assertions(2)

        handler addJson

        handler infoJson

        expect.Invoke(``end``.LastCall).toMatchSnapshot()

        handler removeJson

        handler infoJson

        ``end`` <?> Some("{}");
  ]
]
