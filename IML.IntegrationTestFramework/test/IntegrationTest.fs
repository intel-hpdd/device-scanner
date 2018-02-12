// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTestFramework.IntegrationTest

open Fable.Import.Jest.Matchers
open IML.StatefulPromise.StatefulPromise
open IML.IntegrationTestFramework.IntegrationTestFramework
open Fable.Import.Node.PowerPack
open Fable.PowerPack
open Fable.Import.Jest

let private rb cnt () =
 execShell (sprintf "adf%d" cnt)

let private doCmd x cnt =
  cmd x
    >> rollback (rb cnt)
    >> ignoreCmd

testAsync "Stateful Promise should rollback starting with the last command" <| fun () ->
  expect.assertions 2

  command {
        do! doCmd "rm -f /tmp/integration_test.txt && tou /tmp/integration_test.txt" 0
        do! doCmd "echo 'hello'" 1
        do! doCmd "echo 'goodbye'" 2
        do! doCmd "echo 'another command'" 3
        do! doCmd "echo 'done'" 4

        return! cmd "cat /tmp/integration_test.txt"
      }
        |> run ([], [])
        |> Promise.bind (fun x ->
          x == (Stdout(""), Stderr(""))

          promise {
            let! x = execShell "cat /tmp/integration_test.txt"

            match x with
              | Ok y ->
                y == (Stdout("rollback4\nrollback3\nrollback2\nrollback1\n"), Stderr(""))
              | Error (e, _, _) ->
                failwithf "Error reading from /tmp/integration_test.txt %s" e.message
          }
        )
