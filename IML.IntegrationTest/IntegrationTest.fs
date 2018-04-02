// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTest.IntegrationTest

open Fable.PowerPack
open Fable.Import
open Fable.Import.Node.PowerPack
open IML.StatefulPromise.StatefulPromise
open IML.IntegrationTestFramework.IntegrationTestFramework

open Fable.Import.Jest
open Matchers

let settle () =
  cmd "udevadm settle"
    >> ignoreCmd

let scannerInfo =
  (fun _ -> pipeToShellCmd "echo '\"Stream\"'" "socat - UNIX-CONNECT:/var/run/device-scanner.sock")
    >>= settle()

let resultOutput: StatefulResult<State, Out, Err> -> string = function
  | Ok ((Stdout(r), _), _) -> r
  | Error (e) -> failwithf "Command failed: %A" e

testAsync "stream event" <| fun () ->
  command {
    return! scannerInfo
  }
  |> startCommand "Stream Event"
  |> Promise.map (fun (r, _) ->
      let json =
        r
          |> resultOutput
          |> JS.JSON.parse

      toMatchSnapshot json
  )
