module IML.DeviceScannerDaemon.HandlersTest

open IML.DeviceScannerDaemon.Handlers
open TestFixtures
open Fable.Import.Jest
open Fable.Import.Node
open Matchers

let private evaluate handler (end':Matcher<Buffer.Buffer option, unit>) =
  handler infoUdevJson
  expect.Invoke(end'.LastCall |> Option.map (fun x -> x.toString())).toMatchSnapshot()

testList "Data Handler" [
  let withSetup f ():unit =
    let ``end`` = Matcher<Buffer.Buffer option, unit>()

    let handler = dataHandler ``end``.Mock

    f (``end``, handler)

  yield! testFixture withSetup [
    "Should call end with map for info event", fun (``end``, handler) ->
      evaluate handler ``end``

    "Should call end for add event", fun (``end``, handler) ->
      handler addUdevJson
      ``end`` <?> None;

    "Should call end for add event", fun (``end``, handler) ->
      handler changeUdevJson
      ``end`` <?> None;

    "Should call end for remove event", fun (``end``, handler) ->
      handler removeUdevJson
      ``end`` <?> None;

    "Should end on a bad match", fun (``end``, handler) ->
      expect.assertions 2
      expect.Invoke(fun () -> handler (toJson """{}""")).toThrowErrorMatchingSnapshot()
      ``end`` <?> None;

    "Should add then remove a device path", fun (``end``, handler) ->
      expect.assertions 2
      handler addUdevJson
      evaluate handler ``end``

      handler removeUdevJson
      evaluate handler ``end``;

    "Should call end for add pool zed event", fun (``end``, handler) ->
      handler createZpoolJson
      ``end`` <?> None;

    "Should call end for remove pool zed event", fun (``end``, handler) ->
      handler destroyZpoolJson
      ``end`` <?> None;

    "Should call end for import pool zed event", fun (``end``, handler) ->
      handler importZpoolJson
      ``end`` <?> None;

    "Should call end for export pool zed event", fun (``end``, handler) ->
      handler exportZpoolJson
      ``end`` <?> None;

    "Should call end for add dataset zed event", fun (``end``, handler) ->
      handler createZdatasetJson
      ``end`` <?> None;

    "Should call end for remove dataset zed event", fun (``end``, handler) ->
      handler destroyZdatasetJson
      ``end`` <?> None;

    "Should add then remove a zpool", fun (``end``, handler) ->
      expect.assertions 2

      handler createZpoolJson
      evaluate handler ``end``

      handler destroyZpoolJson
      evaluate handler ``end``;

    "Should import then export then import a zpool", fun (``end``, handler) ->
      expect.assertions 3

      handler importZpoolJson
      evaluate handler ``end``

      handler exportZpoolJson
      evaluate handler ``end``

      handler importZpoolJson
      evaluate handler ``end``;

    "Should add then remove a zdataset", fun (``end``, handler) ->
      expect.assertions 2

      handler createZpoolJson
      handler createZdatasetJson
      evaluate handler ``end``

      handler destroyZdatasetJson
      evaluate handler ``end``;

    "Should export then import zpool with datasets", fun (``end``, handler) ->
      expect.assertions 4

      handler createZpoolJson
      handler createZdatasetJson
      handler exportZpoolJson
      evaluate handler ``end``

      handler importZpoolJson
      evaluate handler ``end``

      handler destroyZdatasetJson
      handler exportZpoolJson
      evaluate handler ``end``

      handler importZpoolJson
      evaluate handler ``end``;

    "Should add pool property then export then import", fun (``end``, handler) ->
      expect.assertions 4

      handler createZpoolJson
      handler createZpoolPropertyJson
      evaluate handler ``end``

      handler exportZpoolJson
      evaluate handler ``end``

      handler importZpoolJson
      evaluate handler ``end``

      handler destroyZpoolJson
      evaluate handler ``end``;

    "Should add dataset property then export then import", fun (``end``, handler) ->
      expect.assertions 4

      handler createZpoolJson
      handler createZdatasetJson
      handler createZdatasetPropertyJson
      evaluate handler ``end``

      handler exportZpoolJson
      evaluate handler ``end``

      handler importZpoolJson
      evaluate handler ``end``

      handler destroyZdatasetJson
      evaluate handler ``end``;

    "Should add multiple pool properties then add two datasets with multiple properties then export then import", fun (``end``, handler) ->
      expect.assertions 5

      handler createZpoolJson
      handler createZdatasetJson
      handler createZdatasetPropertyJson
      handler createZdatasetPropertyTwoJson
      handler createSecondZdatasetJson
      handler createSecondZdatasetPropertyJson
      handler createSecondZdatasetPropertyTwoJson
      handler createZpoolPropertyJson
      handler createZpoolPropertyTwoJson
      evaluate handler ``end``

      handler exportZpoolJson
      evaluate handler ``end``

      handler importZpoolJson
      evaluate handler ``end``

      handler resetZpoolPropertyJson
      handler resetZdatasetPropertyJson
      evaluate handler ``end``

      handler destroyZpoolJson
      evaluate handler ``end``;
    ]
]
