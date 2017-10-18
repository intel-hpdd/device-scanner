module IML.DeviceScannerDaemon.ZFSEventTypesTest

open IML.DeviceScannerDaemon.TestFixtures
open ZFSEventTypes
open Fable.PowerPack
open Fable.Import.Jest
open Matchers

let toJson =  Json.ofString >> Result.unwrapResult

let createAddPoolEventJson = createEventJson createZpool

let addZpool = createAddPoolEventJson (id)

let createRemovePoolEventJson = createEventJson destroyZpool

let removeZpool = createRemovePoolEventJson (id)

let createExportPoolEventJson = createEventJson exportZpool

let exportZpool = createExportPoolEventJson (id)

let createImportPoolEventJson = createEventJson importZpool

let importZpool = createImportPoolEventJson (id)

let createAddDatasetEventJson = createEventJson createZdataset

let addZdataset = createAddDatasetEventJson (id)

let createRemoveDatasetEventJson = createEventJson destroyZdataset

let removeZdataset = createRemoveDatasetEventJson (id)

//let addDiskObj = createAddEventJson (fun x ->
//  x
//    |> Map.add "DEVTYPE" (Json.String("disk")))
//
//let addDmDiskObj = createAddEventJson (fun x ->
//  x
//    |> Map.add "DEVTYPE" (Json.String("disk"))
//    |> Map.add "DM_UUID" (Json.String("LVM-KHoa9g8GBwQJMHjQtL77pGj6b9R1YWrlEDy4qFTQ3cgVnmyhy1zB2cJx2l5yE26D"))
//    |> Map.add "IML_DM_SLAVE_MMS" (Json.String("8:16 8:32"))
//    |> Map.add "IML_DM_VG_SIZE" (Json.String("  21466447872B")))
//
//let addInvalidDevTypeObj = createAddEventJson (fun x ->
//  x
//    |> Map.add "DEVTYPE" (Json.String("invalid")))
//
//let missingDevNameObj = createAddEventJson (fun x ->
//  x
//    |> Map.remove "DEVNAME")
//
//let floatDevTypeObj = createAddEventJson (fun x ->
//  x
//    |> Map.add "DEVTYPE" (Json.Number(7.0)))

let addHistoryMatch = function
  | ZedHistoryEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

let poolCreateMatch = function
  | ZedPoolCreateEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

let poolDestroyMatch = function
  | ZedPoolDestroyEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

let poolExportMatch = function
  | ZedPoolExportEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

let poolImportMatch = function
  | ZedPoolImportEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

let datasetCreateMatch = function
  | ZedDatasetCreateEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

let datasetDestroyMatch = function
  | ZedDatasetDestroyEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

test "Matching Events" <| fun () ->
  expect.assertions 6

  expect.Invoke(poolCreateMatch addZpool).toMatchSnapshot()

  expect.Invoke(poolDestroyMatch removeZpool).toMatchSnapshot()

  expect.Invoke(poolExportMatch exportZpool).toMatchSnapshot()

  expect.Invoke(poolImportMatch importZpool).toMatchSnapshot()

  expect.Invoke(datasetCreateMatch addZdataset).toMatchSnapshot()

  expect.Invoke(datasetDestroyMatch removeZdataset).toMatchSnapshot()

//  expect.Invoke(infoMatch (toJson """{ "ACTION": "info" }""")).toMatchSnapshot()
//
//  try
//    addMatch (toJson """{ "ACTION": "blah" }""") |> ignore
//  with
//    | msg ->
//      msg.Message === "No Match"
//
//  try
//    addMatch addInvalidDevTypeObj |> ignore
//  with
//    | msg ->
//      msg.Message === "DEVTYPE neither partition or disk"
//
//  try
//    addMatch missingDevNameObj |> ignore
//  with
//    | msg ->
//      expect.Invoke(msg.Message).toMatchSnapshot()
//
//  try
//    addMatch floatDevTypeObj |> ignore
//  with
//    msg ->
//      msg.Message === "Invalid JSON, it must be a string"
