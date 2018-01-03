module IML.DeviceScannerDaemon.UdevTest

open IML.DeviceScannerDaemon.TestFixtures
open Udev
open Fable.Import.Jest
open Matchers

let addMatch = function
  | UdevAdd x -> Some x
  | _ -> None

let removeMatch = function
  | UdevRemove x -> Some x
  | _ -> None

test "Matching Events" <| fun () ->
  expect.assertions 6

  toMatchSnapshot (addMatch addUdevJson)

  toMatchSnapshot (addMatch addDiskUdevJson)

  toMatchSnapshot (addMatch addDmUdevJson)

  toMatchSnapshot (removeMatch removeUdevJson)

  toMatchSnapshot (addMatch (toJson """{ "ACTION": "blah" }"""))

  toMatchSnapshot (addMatch addMdraidUdevJson)
