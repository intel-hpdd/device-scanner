// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTest.IntegrationTest

open Fable.Import.Jest.Matchers
open Fable.Core.JsInterop
open Fable.PowerPack
open IML.StatefulPromise.StatefulPromise
open IML.IntegrationTestFramework.IntegrationTestFramework

open Fable.Import.Jest
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open Fable.PowerPack.Json

let scannerInfo =
  pipeToShellCmd "echo '\"Stream\"'" "socat - UNIX-CONNECT:/var/run/device-scanner.sock"
let unwrapObject a =
    match a with
    | Json.Object a -> Map.ofArray a
    | _ -> failwith "Invalid JSON, it must be an object"

let unwrapResult a =
  match a with
  | Ok x -> x
  | Error e -> failwith !!e

let unwrapDeviceData = Json.ofString >> unwrapResult >> unwrapObject >> Map.find("blockDevices") >> unwrapObject
let resultOutput: StatefulResult<State, Out, Err> -> string = function
  | Ok ((Stdout(r), _), _) -> r
  | Error (e) -> failwithf "Command failed: %A" !!e

testAsync "stream event" <| fun () ->
  command {
    return! scannerInfo
  }
  |> startCommand "Stream Event"
  |> Promise.map (fun (r, _) ->
      let json =
        r
          |> resultOutput
          |> unwrapDeviceData
          |> toJson
          |> buffer.Buffer.from

      toMatchSnapshot json
  )
