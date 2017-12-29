module IML.DeviceScannerDaemon.UdevTest

open IML.DeviceScannerDaemon.TestFixtures
open Udev
open Fable.Import.Jest
open Matchers

// let addDmObj =
//   addObj
//     |> Map.add "DEVTYPE" (Json.String("disk"))
//     |> Map.add "DM_UUID" (Json.String("LVM-KHoa9g8GBwQJMHjQtL77pGj6b9R1YWrlEDy4qFTQ3cgVnmyhy1zB2cJx2l5yE26D"))
//     |> Map.add "IML_DM_SLAVE_MMS" (Json.String("8:16 8:32"))
//     |> Map.add "IML_DM_VG_SIZE" (Json.String("  21466447872B"))

// let addInvalidDevTypeObj =
//   addObj
//     |> Map.add "DEVTYPE" (Json.String("invalid"))

// let missingDevNameObj =
//   addObj
//     |> Map.remove "DEVNAME"

// let floatDevTypeObj =
//   addObj
//     |> Map.add "DEVTYPE" (Json.Number(7.0))

let addMatch = function
  | UdevAdd x -> Some x
  | _ -> None

let removeMatch = function
  | UdevRemove x -> Some x
  | _ -> None

test "Matching Events" <| fun () ->
  expect.assertions 11

  toMatchSnapshot (addMatch addObj)

  toMatchSnapshot (addMatch addMdraidJson)

  // toMatchSnapshot (addMatch addDmObj)

  toMatchSnapshot (removeMatch removeJson)

  toMatchSnapshot (addMatch (toJson """{ "ACTION": "blah" }"""))

  // toMatchSnapshot (addObj |> Map.add "IML_IS_RO" (Json.String "0") |> addMatch)

  // toMatchSnapshot (addObj |> Map.add "ID_FS_TYPE" (Json.String "") |> addMatch)

  // expect.Invoke(fun () -> addMatch addInvalidDevTypeObj).toThrowErrorMatchingSnapshot()

  // expect.Invoke(fun () -> addMatch missingDevNameObj).toThrowErrorMatchingSnapshot()

  // expect.Invoke(fun () -> addMatch floatDevTypeObj).toThrowErrorMatchingSnapshot()

  toMatchSnapshot (addMatch addMdraidJson)
