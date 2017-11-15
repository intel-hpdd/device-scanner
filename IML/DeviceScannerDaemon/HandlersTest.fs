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
let private evaluate handler (end':Matcher<string option, unit>) =
  handler infoJson
  expect.Invoke(end'.LastCall).toMatchSnapshot()

testList "Data Handler" [
  let withSetup f ():unit =
    let ``end`` = Matcher<string option, unit>()

    let handler = dataHandler ``end``.Mock

    f (``end``, handler)

  yield! testFixture withSetup [
    "Should call end with map for info event", fun (``end``, handler) ->
      evaluate handler ``end``

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
      ``end`` <?> None;

    "Should add then remove a device path", fun (``end``, handler) ->
      expect.assertions 2
      handler addJson
      evaluate handler ``end``

      handler removeJson
      evaluate handler ``end``;

    "Should call end for add pool zed event", fun (``end``, handler) ->
      handler (mapToJson createZpool)
      ``end`` <?> None;

    "Should call end for remove pool zed event", fun (``end``, handler) ->
      handler (mapToJson destroyZpool)
      ``end`` <?> None;

    "Should call end for import pool zed event", fun (``end``, handler) ->
      handler (mapToJson importZpool)
      ``end`` <?> None;

    "Should call end for export pool zed event", fun (``end``, handler) ->
      handler (mapToJson exportZpool)
      ``end`` <?> None;

    "Should call end for add dataset zed event", fun (``end``, handler) ->
      handler (mapToJson createZdataset)
      ``end`` <?> None;

    "Should call end for remove dataset zed event", fun (``end``, handler) ->
      handler (mapToJson destroyZdataset)
      ``end`` <?> None;

    "Should add then remove a zpool", fun (``end``, handler) ->
      expect.assertions 2
      let handleJson = mapToJson >> handler

      handleJson createZpool
      evaluate handler ``end``

      handleJson destroyZpool
      evaluate handler ``end``;

    "Should import then export then import a zpool", fun (``end``, handler) ->
      expect.assertions 3
      let handleJson = mapToJson >> handler

      handleJson importZpool
      evaluate handler ``end``

      handleJson exportZpool
      evaluate handler ``end``

      handleJson importZpool
      evaluate handler ``end``;

    "Should add then remove a zdataset", fun (``end``, handler) ->
      expect.assertions 2
      let handleJson = mapToJson >> handler

      handleJson createZpool
      handleJson createZdataset
      evaluate handler ``end``

      handleJson destroyZdataset
      evaluate handler ``end``;

    "Should export then import zpool with datasets", fun (``end``, handler) ->
      expect.assertions 4
      let handleJson = mapToJson >> handler

      handleJson createZpool
      handleJson createZdataset
      handleJson exportZpool
      evaluate handler ``end``

      handleJson importZpool
      evaluate handler ``end``

      handleJson destroyZdataset
      handleJson exportZpool
      evaluate handler ``end``

      handleJson importZpool
      evaluate handler ``end``;
  ]
]
