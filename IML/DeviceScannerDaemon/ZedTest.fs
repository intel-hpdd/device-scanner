module IML.DeviceScannerDaemon.ZedTest

open IML.DeviceScannerDaemon
open TestFixtures
open Zed
open Fable.Import.Jest
open Matchers

let poolCreateMatch = function
  | Zpool.Create x -> Some x
  | _ -> None

let poolDestroyMatch = function
  | Zpool.Destroy x -> Some x
  | _ -> None

let poolExportMatch = function
  | Zpool.Export x -> Some x
  | _ -> None

let poolImportMatch = function
  | Zpool.Import x -> Some x
  | _ -> None

let datasetCreateMatch = function
  | Zfs.Create x -> Some x
  | _ -> None

let datasetDestroyMatch = function
  | Zfs.Destroy x -> Some x
  | _ -> None

test "Matching Events" <| fun () ->
  expect.assertions 6

  toMatchSnapshot (poolCreateMatch createZpool)

  toMatchSnapshot (poolDestroyMatch destroyZpool)

  toMatchSnapshot (poolExportMatch exportZpool)

  toMatchSnapshot (poolImportMatch importZpool)

  toMatchSnapshot (datasetCreateMatch createZdataset)

  toMatchSnapshot (datasetDestroyMatch destroyZdataset)