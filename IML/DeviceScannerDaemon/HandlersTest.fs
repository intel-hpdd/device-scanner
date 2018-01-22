module IML.DeviceScannerDaemon.HandlersTest

open IML.DeviceScannerDaemon.Handlers
open TestFixtures
open Fable.Import.Jest
open Matchers

testList "Data Handler" [
  let withSetup f ():unit =
    f (handler)

  yield! testFixture withSetup [
    "Should call end with map for info event", fun (handler) ->
      handler infoUdevJson
        |> toMatchSnapshot


    "Should end on a bad match", fun (handler) ->
      expect.assertions 1
      expect.Invoke(fun () -> handler (toJson """{}""")).toThrowErrorMatchingSnapshot()

    "Should add then remove a device path", fun (handler) ->
      expect.assertions 2
      handler addUdevJson |> toMatchSnapshot

      handler removeUdevJson |> toMatchSnapshot

    "Should add then remove a zpool", fun (handler) ->
      expect.assertions 2

      handler createZpoolJson
        |> toMatchSnapshot

      handler destroyZpoolJson
        |> toMatchSnapshot

    "Should import then export then import a zpool", fun (handler) ->
      expect.assertions 3

      handler importZpoolJson
        |> toMatchSnapshot

      handler exportZpoolJson
        |> toMatchSnapshot

      handler importZpoolJson
        |> toMatchSnapshot

    "Should add then remove a zdataset", fun (handler) ->
      expect.assertions 2

      handler createZpoolJson
        |> ignore
      handler createZdatasetJson
        |> toMatchSnapshot

      handler destroyZdatasetJson
        |> toMatchSnapshot

    "Should export then import zpool with datasets", fun (handler) ->
      expect.assertions 4

      handler createZpoolJson
        |> ignore
      handler createZdatasetJson
        |> ignore
      handler exportZpoolJson
        |> toMatchSnapshot

      handler importZpoolJson
        |> toMatchSnapshot

      handler destroyZdatasetJson
        |> ignore
      handler exportZpoolJson
        |> toMatchSnapshot

      handler importZpoolJson
        |> toMatchSnapshot

    "Should add pool property then export then import", fun (handler) ->
      expect.assertions 4

      handler createZpoolJson
        |> ignore
      handler createZpoolPropertyJson
        |> toMatchSnapshot

      handler exportZpoolJson
        |> toMatchSnapshot

      handler importZpoolJson
        |> toMatchSnapshot

      handler destroyZpoolJson
        |> toMatchSnapshot

    "Should add dataset property then export then import", fun (handler) ->
      expect.assertions 4

      handler createZpoolJson
        |> ignore
      handler createZdatasetJson
        |> ignore
      handler createZdatasetPropertyJson
        |> toMatchSnapshot

      handler exportZpoolJson
        |> toMatchSnapshot

      handler importZpoolJson
        |> toMatchSnapshot

      handler destroyZdatasetJson
        |> toMatchSnapshot

    "Should add multiple pool properties then add two datasets with multiple properties then export then import", fun (handler) ->
      expect.assertions 5

      handler createZpoolJson |> ignore
      handler createZdatasetJson |> ignore
      handler createZdatasetPropertyJson |> ignore
      handler createZdatasetPropertyTwoJson |> ignore
      handler createSecondZdatasetJson |> ignore
      handler createSecondZdatasetPropertyJson |> ignore
      handler createSecondZdatasetPropertyTwoJson |> ignore
      handler createZpoolPropertyJson |> ignore
      handler createZpoolPropertyTwoJson
        |> toMatchSnapshot

      handler exportZpoolJson
        |> toMatchSnapshot

      handler importZpoolJson
        |> toMatchSnapshot

      handler resetZpoolPropertyJson
        |> ignore
      handler resetZdatasetPropertyJson
        |> toMatchSnapshot

      handler destroyZpoolJson
        |> toMatchSnapshot
    ]
]
