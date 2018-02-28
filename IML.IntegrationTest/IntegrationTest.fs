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
open Fable.Import.Node.PowerPack

let tap f x = f x; x
let scannerInfo =
  pipeToShellCmd "echo '\"Info\"'" "socat - UNIX-CONNECT:/var/run/device-scanner.sock"
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

testAsync "info event" <| fun () ->
  command {
    return! scannerInfo
  }
  |> startCommand
  |> Promise.map (tap (logCommands "Info Event"))
  |> Promise.map (tap (fun (r, _) ->
      let json =
        r
          |> resultOutput
          |> unwrapDeviceData
          |> Map.filter (fun key _ ->
            key <> "/devices/virtual/block/dm-0" &&
            key <> "/devices/virtual/block/dm-1" &&
            key <> "/devices/virtual/block/dm-2" &&
            key <> "/devices/virtual/block/dm-3"
          )
          |> sprintf "%A"

      toMatchSnapshot json
    )
  )
