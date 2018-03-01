// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTestFramework.IntegrationTest

open Fable.Import.Jest.Matchers
open IML.StatefulPromise.StatefulPromise
open IML.IntegrationTestFramework.IntegrationTestFramework
open Fable.Core.JsInterop
open Fable.Import.Node.PowerPack
open Fable.PowerPack
open Fable.Import
open Fable.Import.Jest

let private rb (cnt:int) (rollbackState:RollbackState): JS.Promise<RollbackResult<Out, Err>> =
  let cmd = (sprintf "echo \"rollback%d\" >> /tmp/integration_test.txt" cnt)
  cmd |> (execShell >> (addToRollbackState cmd rollbackState))

let private doRbCmd (x:string) (cnt:int): State -> JS.Promise<CommandResult<unit, Err>> =
  cmd x
    >> rollback (rb cnt)
    >> ignoreCmd

let private doCmd (x:string): State -> JS.Promise<CommandResult<unit, Err>> =
  cmd x
    >> ignoreCmd

let rbCmd (x:string) (cnt:int): State -> JS.Promise<CommandResult<Out, Err>> =
  cmd x
    >> rollback (rb cnt)

let badRb (rollbackState:RollbackState): JS.Promise<RollbackResult<Out, Err>> =
  let cmd = "ech \"badcommand\" >> /tmp/integration_test.txt"
  cmd |> (execShell >> (addToRollbackState cmd rollbackState))

let private doBadRbCmd (x:string): State -> JS.Promise<CommandResult<unit, Err>> =
  cmd x
    >> rollback badRb
    >> ignoreCmd
let tap f x = f x; x

testAsync "Stateful Promise should rollback starting with the last command" <| fun () ->
  expect.assertions 4

  command {
        do! doRbCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt" 0
        do! doRbCmd "echo \"hello\"" 1
        do! doRbCmd "echo \"goodbye\"" 2
        do! doRbCmd "echo \"another command\"" 3
        return! rbCmd "echo \"done\"" 4
      }
        |> startCommand "Stateful Promise should rollback starting with the last command"
        |> Promise.bind (fun (commandResult, rollbackResult) ->
          match rollbackResult with
            | Ok (_, logs) ->
              logs |> mapRollbackResultToString  == [
                ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
                ("echo \"hello\"", "hello\n");
                ("echo \"goodbye\"", "goodbye\n");
                ("echo \"another command\"", "another command\n");
                ("echo \"done\"", "done\n");
                ("echo \"rollback4\" >> /tmp/integration_test.txt", "");
                ("echo \"rollback3\" >> /tmp/integration_test.txt", "");
                ("echo \"rollback2\" >> /tmp/integration_test.txt", "");
                ("echo \"rollback1\" >> /tmp/integration_test.txt", "");
                ("echo \"rollback0\" >> /tmp/integration_test.txt", "")
              ]
            | Error (e, _) -> failwithf "Logs should not contain an error. %A" e


          match commandResult with
            | Ok ((Stdout(cmdResult), _), (logs, _)) ->
              logs |> mapRollbackResultToString == [
                ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
                ("echo \"hello\"", "hello\n");
                ("echo \"goodbye\"", "goodbye\n");
                ("echo \"another command\"", "another command\n");
                ("echo \"done\"", "done\n")
              ]
              cmdResult == "done\n"
            | Error (e, _) ->
              failwithf "The last command should not be an error: %A" e

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
  expect.assertions 3

  command {
        do! doRbCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt" 0
        do! doRbCmd "echo \"hello\"" 1
        do! doRbCmd "ech \"goodbye\"" 2
        do! doRbCmd "echo \"another command\"" 3
        return! rbCmd "echo \"done\"" 4
      }
        |> startCommand "Stateful Promise should stop executing commands and rollback when an error occurs"
        |> Promise.bind (fun (commandResult, rollbackResult) ->
          match rollbackResult with
            Ok (_, logs) ->
              logs |> mapRollbackResultToString == [
                ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
                ("echo \"hello\"", "hello\n");
                ("ech \"goodbye\"", "{\"killed\":false,\"code\":127,\"signal\":null,\"cmd\":\"ssh devicescannernode 'ech \\\"goodbye\\\"'\"} - \"bash: ech: command not found\\n\"");
                ("echo \"rollback2\" >> /tmp/integration_test.txt", "");
                ("echo \"rollback1\" >> /tmp/integration_test.txt", "");
                ("echo \"rollback0\" >> /tmp/integration_test.txt", "");
              ]
            | Error (e, _) -> failwithf "Rollbacks should not have had an error: %A" e

          match commandResult with
            | Ok (_) ->
              failwithf "Command result should have matched the error case."
            | Error (e, _) ->
              let myError = e |> errToString
              myError == "{\"killed\":false,\"code\":127,\"signal\":null,\"cmd\":\"ssh devicescannernode 'ech \\\"goodbye\\\"'\"} - \"bash: ech: command not found\\n\""

          promise {
            let! x = execShell "cat /tmp/integration_test.txt"

            match x with
              | Ok y ->
                y == (Stdout("rollback2\nrollback1\nrollback0\n"), Stderr(""))
              | Error (e, _, _) ->
                failwithf "Error reading from /tmp/integration_test.txt %s" e.message
          }
        )

testAsync "Stateful promise should log commands and rollback commands when an error occurs during rollback" <| fun () ->
  expect.assertions 4

  command {
    do! doRbCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt" 0
    do! doRbCmd "echo \"command\"" 1
    do! doBadRbCmd "echo \"a command with a bad rollback\""
    return! rbCmd "echo \"final command\"" 2
  }
    |> startCommand "Stateful promise should log commands and rollback commands when an error occurs during rollback"
    |> Promise.bind (fun (commandResult, rollbackResult) ->
      match rollbackResult with
        Ok (_) ->
          failwithf "Rollbacks should have hit the error case:"
        | Error (_, logs) ->
          logs |> mapRollbackResultToString == [
            ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
            ("echo \"command\"", "command\n");
            ("echo \"a command with a bad rollback\"", "a command with a bad rollback\n");
            ("echo \"final command\"", "final command\n");
            ("echo \"rollback2\" >> /tmp/integration_test.txt", "");
            ("ech \"badcommand\" >> /tmp/integration_test.txt", "{\"killed\":false,\"code\":127,\"signal\":null,\"cmd\":\"ssh devicescannernode 'ech \\\"badcommand\\\" >> /tmp/integration_test.txt'\"} - \"bash: ech: command not found\\n\"")
          ]

      match commandResult with
        | Ok ((Stdout(cmdResult), _), (logs, _)) ->
          logs |> mapRollbackResultToString == [
            ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
            ("echo \"command\"", "command\n");
            ("echo \"a command with a bad rollback\"", "a command with a bad rollback\n");
            ("echo \"final command\"", "final command\n");
          ]
          cmdResult == "final command\n"
        | Error (e, _) ->
          failwithf "The last command should not be an error: %A" !!e


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
  expect.assertions 4

  command {
    do! doCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt"
    do! doRbCmd "echo \"command1\"" 0
    return! cmd "echo \"command2\""
  }
    |> startCommand "Stateful promise should log commands and single rollback command when there is only 1 rollback"
    |> Promise.bind (fun (commandResult, rollbackResult) ->
      match rollbackResult with
        Ok (_, logs) ->
          logs |> mapRollbackResultToString == [
            ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
            ("echo \"command1\"", "command1\n");
            ("echo \"command2\"", "command2\n");
            ("echo \"rollback0\" >> /tmp/integration_test.txt", "");
          ]
        | Error (e, _) -> failwithf "Rollbacks should not have had an error: %A" e

      match commandResult with
        | Ok ((Stdout(cmdResult), _), (logs, _)) ->
          logs |> mapRollbackResultToString == [
            ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
            ("echo \"command1\"", "command1\n");
            ("echo \"command2\"", "command2\n");
          ]
          cmdResult == "command2\n"
        | Error (e, _) -> failwithf "The last command should not be an error: %A" !!e

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
  expect.assertions 4

  command {
    do! doCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt"
    do! doBadRbCmd "echo \"command1\""
    return! cmd "echo \"command2\""
  }
    |> startCommand "Stateful promise should log commands and rollback error when the only rollback fails"
    |> Promise.bind (fun (commandResult, rollbackResult) ->
      match rollbackResult with
        Ok (_) ->
          failwithf "Rollbacks should have hit the error case:"
        | Error (_, logs) ->
          logs |> mapRollbackResultToString == [
            ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
            ("echo \"command1\"", "command1\n");
            ("echo \"command2\"", "command2\n");
            ("ech \"badcommand\" >> /tmp/integration_test.txt", "{\"killed\":false,\"code\":127,\"signal\":null,\"cmd\":\"ssh devicescannernode 'ech \\\"badcommand\\\" >> /tmp/integration_test.txt'\"} - \"bash: ech: command not found\\n\"")
          ]

      match commandResult with
        | Ok ((Stdout(cmdResult), _), (logs, _)) ->
          logs |> mapRollbackResultToString == [
            ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
            ("echo \"command1\"", "command1\n");
            ("echo \"command2\"", "command2\n");
          ]
          cmdResult == "command2\n"
        | Error (e, _) ->
          failwithf "The last command should not be an error: %A" !!e

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
  expect.assertions 4

  command {
    do! doCmd "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt"
    do! doCmd "echo \"command1\""
    return! cmd "echo \"command2\""
  }
    |> startCommand "Stateful promise should log commands when there are no rollbacks"
    |> Promise.bind (fun (commandResult, rollbackResult) ->
      match rollbackResult with
        Ok (_, logs) ->
          logs |> mapRollbackResultToString == [
            ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
            ("echo \"command1\"", "command1\n");
            ("echo \"command2\"", "command2\n");
          ]
        | Error (e, _) -> failwithf "Rollbacks should not have had an error: %A" e

      match commandResult with
        | Ok ((Stdout(cmdResult), _), (logs, _)) ->
          logs |> mapRollbackResultToString == [
            ("rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt", "");
            ("echo \"command1\"", "command1\n");
            ("echo \"command2\"", "command2\n");
          ]
          cmdResult == "command2\n"
        | Error (e, _) ->
          failwithf "The last command should not be an error: %A" !!e

      promise {
        let! x = execShell "cat /tmp/integration_test.txt"

        match x with
          | Ok y ->
            y == (Stdout(""), Stderr(""))
          | Error (e, _, _) ->
            failwithf "Error reading from /tmp/integraton_test.txt %s" e.message
      }
    )
