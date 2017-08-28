module IML.DeviceScannerDaemon.ParseUdevDBTest

open Fable.Import
open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open Fable.Core.JsInterop
open Fable.Import.Node
open Fable.PowerPack

open IML.DeviceScannerDaemon.ParseUdevDB

let fixture = Fs.readFileSync("./IML/DeviceScannerDaemon/udev-db-fixture.txt").toString()


test "it should parse the udev db into an Array of Maps" <| fun () ->
  expect.assertions 2

  let xs = parser fixture

  Array.length xs == 13

  expect.Invoke(xs).toMatchSnapshot()


describe "getUdevDb" <| fun () ->
  let mutable mockExec = null
  let mutable parseUdevDB = null

  beforeEach <| fun () ->
    mockExec <- Matcher3<string, obj, (obj option -> obj -> obj -> unit), unit>()

    jest.mock("child_process", fun () -> createObj ["exec" ==> mockExec.Mock])

    parseUdevDB <- Globals.require.Invoke "./ParseUdevDB.fs"

  test "should invoke ChildProcess.exec" <| fun () ->
    parseUdevDB?getUdevDB()
      |> ignore

    let (command, _, _) = mockExec.LastCall

    command == "udevadm info -e"

  testAsync "should return a response" <| fun () ->
    let p = parseUdevDB?getUdevDB() :?> JS.Promise<string>

    let (_, _, fn) = mockExec.LastCall

    fn $ (None, "passed", "failed")
      |> ignore

    promise {
      let! r = p

      r == "passed"
    }

  testAsync "should return a rejection" <| fun () ->
    let p = parseUdevDB?getUdevDB() :?> JS.Promise<string>

    let (_, _, fn) = mockExec.LastCall

    fn $ (Some (exn "boom"), "passed", "failed")
      |> ignore

    p
      |> Promise.catch (fun (x:exn) ->
        x == exn "boom"
        ""
      )
