module IML.DeviceScannerDaemon.ZFSEventTypesTest

open IML.DeviceScannerDaemon.TestFixtures
open ZFSEventTypes
open Fable.Import.Jest
open Matchers

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

let datasetCreateMatch = function
  | ZedDataset "create" x -> Some x
  | _ -> None

let datasetDestroyMatch = function
  | ZedDataset "destroy" x -> Some x
  | _ -> None

test "Matching Events" <| fun () ->
  expect.assertions 6

  toMatchSnapshot (poolCreateMatch createZpool)

  toMatchSnapshot (poolDestroyMatch destroyZpool)

  toMatchSnapshot (poolExportMatch exportZpool)

  toMatchSnapshot (poolImportMatch importZpool)

  toMatchSnapshot (datasetCreateMatch createZdataset)

  toMatchSnapshot (datasetDestroyMatch destroyZdataset)
