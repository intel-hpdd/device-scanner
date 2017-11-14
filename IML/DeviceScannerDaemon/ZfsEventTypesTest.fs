module IML.DeviceScannerDaemon.ZFSEventTypesTest

open IML.DeviceScannerDaemon.TestFixtures
open ZFSEventTypes
open Fable.Import.Jest
open Matchers

let addHistoryMatch = function
  | ZedHistory x -> Some x
  | _ -> None

let poolCreateMatch = function
  | ZedPool "create" x -> Some x
  | _ -> None

let poolDestroyMatch = function
  | ZedDestroy x -> Some x
  | _ -> None

let poolExportMatch = function
  | ZedExport x -> Some x
  | _ -> None

let poolImportMatch = function
  | ZedPool "import" x -> Some x
  | _ -> None

// let datasetCreateMatch = function
//   | ZedDatasetCreateEventMatch x -> x
//   | _ -> raise (System.Exception "No Match")

// let datasetDestroyMatch = function
//   | ZedDatasetDestroyEventMatch x -> x
//   | _ -> raise (System.Exception "No Match")

test "Matching Events" <| fun () ->
  expect.assertions 4

  toMatchSnapshot (poolCreateMatch createZpool)

  toMatchSnapshot (poolDestroyMatch destroyZpool)

  toMatchSnapshot (poolExportMatch exportZpool)

  toMatchSnapshot (poolImportMatch importZpool)

  // toMatchSnapshot (datasetCreateMatch addZdataset)

  // toMatchSnapshot (datasetDestroyMatch removeZdataset)
