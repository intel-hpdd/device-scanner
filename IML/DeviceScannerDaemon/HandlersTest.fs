module IML.DeviceScannerDaemon.HandlersTest

open IML.DeviceScannerDaemon.Handlers
open TestFixtures
open Fable.Import.Jest
open Fable.Import.Node
open Matchers

let private evaluate handler (end':Matcher<Buffer.Buffer option, unit>) =
  handler infoJson
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
      handler addObj
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
      handler addObj
      evaluate handler ``end``

      handler removeJson
      evaluate handler ``end``;

    "Should call end for add pool zed event", fun (``end``, handler) ->
      handler createZpool
      ``end`` <?> None;

    "Should call end for remove pool zed event", fun (``end``, handler) ->
      handler destroyZpool
      ``end`` <?> None;

    "Should call end for import pool zed event", fun (``end``, handler) ->
      handler importZpool
      ``end`` <?> None;

    "Should call end for export pool zed event", fun (``end``, handler) ->
      handler exportZpool
      ``end`` <?> None;

    "Should call end for add dataset zed event", fun (``end``, handler) ->
      handler createZdataset
      ``end`` <?> None;

    "Should call end for remove dataset zed event", fun (``end``, handler) ->
      handler destroyZdataset
      ``end`` <?> None;

    "Should add then remove a zpool", fun (``end``, handler) ->
      expect.assertions 2

      handler createZpool
      evaluate handler ``end``

      handler destroyZpool
      evaluate handler ``end``;

    "Should import then export then import a zpool", fun (``end``, handler) ->
      expect.assertions 3

      handler importZpool
      evaluate handler ``end``

      handler exportZpool
      evaluate handler ``end``

      handler importZpool
      evaluate handler ``end``;

    "Should add then remove a zdataset", fun (``end``, handler) ->
      expect.assertions 2

      handler createZpool
      handler createZdataset
      evaluate handler ``end``

      handler destroyZdataset
      evaluate handler ``end``;

    "Should export then import zpool with datasets", fun (``end``, handler) ->
      expect.assertions 4

      handler createZpool
      handler createZdataset
      handler exportZpool
      evaluate handler ``end``

      handler importZpool
      evaluate handler ``end``

      handler destroyZdataset
      handler exportZpool
      evaluate handler ``end``

      handler importZpool
      evaluate handler ``end``;

    "Should add pool property then export then import", fun (``end``, handler) ->
      expect.assertions 4

      handler createZpool
      handler createZpoolProperty
      evaluate handler ``end``

      handler exportZpool
      evaluate handler ``end``

      handler importZpool
      evaluate handler ``end``

      handler destroyZpool
      evaluate handler ``end``;

    "Should add dataset property then export then import", fun (``end``, handler) ->
      expect.assertions 4

      handler createZpool
      handler createZdataset
      handler createZdatasetProperty
      evaluate handler ``end``

      handler exportZpool
      evaluate handler ``end``

      handler importZpool
      evaluate handler ``end``

      handler destroyZdataset
      evaluate handler ``end``;

    "Should add multiple pool properties then add two datasets with multiple properties then export then import", fun (``end``, handler) ->
      expect.assertions 5

      handler createZpool
      handler createZdataset
      handler createZdatasetProperty
      handler createZdatasetPropertyTwo
      handler createSecondZdataset
      handler createSecondZdatasetProperty
      handler createSecondZdatasetPropertyTwo
      handler createZpoolProperty
      handler createZpoolPropertyTwo
      evaluate handler ``end``

      handler exportZpool
      evaluate handler ``end``

      handler importZpool
      evaluate handler ``end``

      handler resetZpoolProperty
      handler resetZdatasetProperty
      evaluate handler ``end``

      handler destroyZpool
      evaluate handler ``end``;

    "Should fail when adding a dataset to non-existent pool", fun (_, handler) ->
      expect.Invoke(fun () -> handler createZdataset).toThrowErrorMatchingSnapshot();

    "Should fail when adding a property to non-existent pool", fun (_, handler) ->
      expect.Invoke(fun () -> handler createZpoolProperty).toThrowErrorMatchingSnapshot();

    "Should fail when adding a property to non-existent dataset on a non-existent pool", fun (_, handler) ->
      expect.Invoke(fun () -> handler createZdatasetProperty).toThrowErrorMatchingSnapshot();

    "Should fail when adding a property to non-existent dataset", fun (_, handler) ->
      handler createZpool
      expect.Invoke(fun () -> handler createZdatasetProperty).toThrowErrorMatchingSnapshot();
    ]
]
