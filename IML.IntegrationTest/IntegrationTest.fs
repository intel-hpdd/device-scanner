module IML.IntegrationTest.IntegrationTest

open Fable.Import.Jest.Matchers
open IML.StatefulMonad.StatefulPromise
open IML.IntegrationTest.IntegrationTestFramework
open Fable.Import.Node.PowerPack
open Fable.Import.Node
open Fable.PowerPack
open Fable.Import.Jest

type ExecOk = Stdout * Stderr
type ExecErr = ChildProcess.ExecError * Stdout * Stderr
type CommandFn = unit -> Fable.Import.JS.Promise<Result<ExecOk, ExecErr>>

let command = StatefulPromise()

let rb1 () =
  ChildProcess.exec (shellCommand "echo 'rollback1' >> /tmp/integration_test.txt") None

let rb2 () =
  ChildProcess.exec (shellCommand "echo 'rollback2' >> /tmp/integration_test.txt") None

let rb3 () =
  ChildProcess.exec (shellCommand "echo 'rollback3' >> /tmp/integration_test.txt") None

let rb4 () =
  ChildProcess.exec (shellCommand "echo 'rollback4' > /tmp/integration_test.txt") None


Exports.testAsync "Stateful Promise should rollback starting with the last command" <| fun () ->
  command {
        let! _ = runTestCommand "rm -f /tmp/integration_test.txt && touch /tmp/integration_test.txt" None
        let! _ = runTestCommand "echo 'hello'" (Some(rb1))
        let! _ = runTestCommand "echo 'goodbye'" (Some(rb2))
        let! _ = runTestCommand "echo 'another command'" (Some(rb3))
        let! _ = runTestCommand "echo 'done'" (Some(rb4))
        ()
      } |> testRun []
      |> Promise.map (fun _ ->
        promise {
          let! x = ChildProcess.exec (shellCommand "cat /tmp/integration_test.txt") None
          x
            |> Result.map(fun (Stdout(result), _) ->
              result == "rollback4\nrollback3\nrollback2\nrollback1\n"
            )
            |> Result.mapError(fun (e, _, _) ->
              let message = sprintf "Error reading from /tmp/integration_test.txt %s" e.message
              raise (System.Exception(message))
            ) |> ignore
        }
      )
