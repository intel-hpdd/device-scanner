module IML.DeviceScannerDaemon.EventTypesTest

open IML.DeviceScannerDaemon.TestFixtures
open IML.DeviceScannerDaemon.EventTypes
open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open Fable.Core.JsInterop
open Fable.PowerPack

let toJson =  Json.ofString >> Result.unwrapResult

let matcher = function
  | InfoEventMatch x -> "infoEvent"
  | AddOrChangeEventMatch x -> "addOrChangeEvent"
  | RemoveEventMatch x -> "removeEvent"
  | _ -> "no match"

test "Matching Events" <| fun () ->
  expect.assertions 4

  matcher addObj === "addOrChangeEvent"

  matcher removeObj === "removeEvent"

  matcher (toJson """{ "ACTION": "info" }""") === "infoEvent"

  matcher (toJson """{ "ACTION": "blah" }""") === "no match"
