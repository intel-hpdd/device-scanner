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

  toMatchSnapshot (addMatch addObj)

  toMatchSnapshot (addMatch addDiskObj)

  toMatchSnapshot (addMatch addDmObj)

  toMatchSnapshot (removeMatch removeJson)

  toMatchSnapshot (addMatch (toJson """{ "ACTION": "blah" }"""))

  toMatchSnapshot (addMatch addMdraidJson)
