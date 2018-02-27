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

let private rb cnt (rollbackState:RollbackState) =
  (sprintf "echo \"rollback%d\" >> /tmp/integration_test.txt" cnt)
    |> (execShell >> addToRollbackState(rollbackState))

let private doRbCmd x cnt =
  cmd x
    >> rollback (rb cnt)
    >> ignoreCmd

let private doCmd x =
  cmd x
    >> ignoreCmd

let badRb (rollbackState:RollbackState) =
  "ech \"badcommand\" >> /tmp/integration_test.txt"
    |> (execShell >> addToRollbackState(rollbackState))

let private doBadRbCmd x =
  cmd x
    >> rollback badRb
    >> ignoreCmd

testAsync "Stateful Promise should rollback starting with the last command" <| fun () ->
  expect.assertions 2

  command {
        do! doRbCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt" 0
        do! doRbCmd "echo \"hello\"" 1
        do! doRbCmd "echo \"goodbye\"" 2
        do! doRbCmd "echo \"another command\"" 3
        do! doRbCmd "echo \"done\"" 4

        return! cmd "cat /tmp/integration_test.txt"
      }
        |> startCommand
        |> Promise.bind (fun l ->
          printfn "l %A: " l
          l == [
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"hello\\n\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"goodbye\\n\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"another command\\n\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"done\\n\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
          ]

          promise {
            let! x = execShell "cat /tmp/integration_test.txt"

            match x with
              | Ok y ->
                y == (Stdout("rollback4\nrollback3\nrollback2\nrollback1\nrollback0\n"), Stderr(""))
              | Error (e, _, _) ->
                failwithf "Error reading from /tmp/integration_test.txt %s" e.message
          }
        )

testAsync "Stateful Promise should stop executing commands and rollback when an error occurs" <| fun () ->
  expect.assertions 2

  command {
        do! doRbCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt" 0
        do! doRbCmd "echo \"hello\"" 1
        do! doRbCmd "ech \"goodbye\"" 2
        do! doRbCmd "echo \"another command\"" 3
        do! doRbCmd "echo \"done\"" 4

        return! cmd "cat /tmp/integration_test.txt"
      }
        |> startCommand
        |> Promise.bind (fun l ->
          l == [
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"hello\\n\"},{\"tag\":0,\"data\":\"\"}]";
            "/{\"killed\":false,\"code\":127,\"signal\":null,\"cmd\":\"ssh devicescannernode 'ech \\\"goodbye\\\"'\"}"
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
            "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
          ]

          promise {
            let! x = execShell "cat /tmp/integration_test.txt"

            match x with
              | Ok y ->
                y == (Stdout("rollback2\nrollback1\nrollback0\n"), Stderr(""))
              | Error (e, _, _) ->
                failwithf "Error reading from /tmp/integration_test.txt %s" e.message
          }
        )

testAsync "Stateful promise should log commands and rollback commands when error occurs during rollback" <| fun () ->
  expect.assertions 2

  command {
    do! doRbCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt" 0
    do! doRbCmd "echo \"command\"" 1
    do! doBadRbCmd "echo \"a command with a bad rollback\""
    do! doRbCmd "echo \"final command\"" 2

    return! cmd "cat /tmp/integration_test.txt"
  }
    |> startCommand
    |> Promise.bind (fun l ->
      l == [
          "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
          "[{\"tag\":0,\"data\":\"command\\n\"},{\"tag\":0,\"data\":\"\"}]";
          "[{\"tag\":0,\"data\":\"a command with a bad rollback\\n\"},{\"tag\":0,\"data\":\"\"}]";
          "[{\"tag\":0,\"data\":\"final command\\n\"},{\"tag\":0,\"data\":\"\"}]";
          "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
          "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
          "/{\"killed\":false,\"code\":127,\"signal\":null,\"cmd\":\"ssh devicescannernode 'ech \\\"badcommand\\\" >> /tmp/integration_test.txt'\"}"
        ]

      promise {
        let! x = execShell "cat /tmp/integration_test.txt"

        match x with
          | Ok y ->
            y == (Stdout("rollback2\n"), Stderr(""))
          | Error (e, _, _) ->
            failwithf "Error reading from /tmp/integraton_test.txt %s" e.message
      }
    )

testAsync "Stateful promise should log commands and single rollback command when there is only 1 rollback" <| fun () ->
  expect.assertions 2

  command {
    do! doCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt"
    do! doRbCmd "echo \"command1\"" 0
    do! doCmd "echo \"command2\""

    return! cmd "cat /tmp/integration_test.txt"
  }
    |> startCommand
    |> Promise.bind (fun l ->
      l == [
        "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]"
        "[{\"tag\":0,\"data\":\"command1\\n\"},{\"tag\":0,\"data\":\"\"}]";
        "[{\"tag\":0,\"data\":\"command2\\n\"},{\"tag\":0,\"data\":\"\"}]";
        "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
        "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]"
        ]

      promise {
        let! x = execShell "cat /tmp/integration_test.txt"

        match x with
          | Ok y ->
            y == (Stdout("rollback0\n"), Stderr(""))
          | Error (e, _, _) ->
            failwithf "Error reading from /tmp/integraton_test.txt %s" e.message
      }
    )

testAsync "Stateful promise should log commands and rollback error when the only rollback fails" <| fun () ->
  expect.assertions 2

  command {
    do! doCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt"
    do! doBadRbCmd "echo \"command1\""
    do! doCmd "echo \"command2\""

    return! cmd "cat /tmp/integration_test.txt"
  }
    |> startCommand
    |> Promise.bind (fun l ->
      l == [
        "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]"
        "[{\"tag\":0,\"data\":\"command1\\n\"},{\"tag\":0,\"data\":\"\"}]";
        "[{\"tag\":0,\"data\":\"command2\\n\"},{\"tag\":0,\"data\":\"\"}]";
        "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]";
        "/{\"killed\":false,\"code\":127,\"signal\":null,\"cmd\":\"ssh devicescannernode 'ech \\\"badcommand\\\" >> /tmp/integration_test.txt'\"}"
        ]

      promise {
        let! x = execShell "cat /tmp/integration_test.txt"

        match x with
          | Ok y ->
            y == (Stdout(""), Stderr(""))
          | Error (e, _, _) ->
            failwithf "Error reading from /tmp/integraton_test.txt %s" e.message
      }
    )

testAsync "Stateful promise should log commands when there are no rollbacks" <| fun () ->
  expect.assertions 2

  command {
    do! doCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt"
    do! doCmd "echo \"command1\""
    do! doCmd "echo \"command2\""

    return! cmd "cat /tmp/integration_test.txt"
  }
    |> startCommand
    |> Promise.bind (fun l ->
      l == [
        "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]"
        "[{\"tag\":0,\"data\":\"command1\\n\"},{\"tag\":0,\"data\":\"\"}]";
        "[{\"tag\":0,\"data\":\"command2\\n\"},{\"tag\":0,\"data\":\"\"}]";
        "[{\"tag\":0,\"data\":\"\"},{\"tag\":0,\"data\":\"\"}]"
        ]

      promise {
        let! x = execShell "cat /tmp/integration_test.txt"

        match x with
          | Ok y ->
            y == (Stdout(""), Stderr(""))
          | Error (e, _, _) ->
            failwithf "Error reading from /tmp/integraton_test.txt %s" e.message
      }
    )
