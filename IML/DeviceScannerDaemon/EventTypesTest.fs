module IML.DeviceScannerDaemon.EventTypesTest

open IML.DeviceScannerDaemon.TestFixtures
open IML.DeviceScannerDaemon.EventTypes
open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open Fable.PowerPack

let toJson =  Json.ofString >> Result.unwrapResult

let createAddEventJson = createEventJson addObj

let addDiskObj = createAddEventJson (fun x ->
  x
    |> Map.add "DEVTYPE" (Json.String("disk")))

let addDmDiskObj = createAddEventJson (fun x ->
  x
    |> Map.add "DEVTYPE" (Json.String("disk"))
    |> Map.add "DM_UUID" (Json.String("LVM-KHoa9g8GBwQJMHjQtL77pGj6b9R1YWrlEDy4qFTQ3cgVnmyhy1zB2cJx2l5yE26D"))
    |> Map.add "IML_DM_SLAVE_MMS" (Json.String("8:16 8:32"))
    |> Map.add "IML_DM_VG_SIZE" (Json.String("  21466447872B")))

let addInvalidDevTypeObj = createAddEventJson (fun x ->
  x
    |> Map.add "DEVTYPE" (Json.String("invalid")))

let missingDevNameObj = createAddEventJson (fun x ->
  x
    |> Map.remove "DEVNAME")

let floatDevTypeObj = createAddEventJson (fun x ->
  x
    |> Map.add "DEVTYPE" (Json.Number(7.0)))

let addMatch = function
  | AddOrChangeEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

let infoMatch = function
  | InfoEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

let removeMatch = function
  | RemoveEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

test "Matching Events" <| fun () ->
  expect.assertions 9

  expect.Invoke(addMatch addObj).toMatchSnapshot()

  expect.Invoke(addMatch addDiskObj).toMatchSnapshot()

  expect.Invoke(addMatch addDmDiskObj).toMatchSnapshot()

  expect.Invoke(removeMatch removeObj).toMatchSnapshot()

  expect.Invoke(infoMatch (toJson """{ "ACTION": "info" }""")).toMatchSnapshot()

  try
    addMatch (toJson """{ "ACTION": "blah" }""") |> ignore
  with
    | msg ->
      msg.Message === "No Match"

  try
    addMatch addInvalidDevTypeObj |> ignore
  with
    | msg ->
      msg.Message === "DEVTYPE neither partition or disk"

  try
    addMatch missingDevNameObj |> ignore
  with
    | msg ->
      expect.Invoke(msg.Message).toMatchSnapshot()

  try
    addMatch floatDevTypeObj |> ignore
  with
    msg ->
      msg.Message === "Invalid JSON, it must be a string"
