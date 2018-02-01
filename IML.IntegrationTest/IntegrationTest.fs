module IML.IntegrationTest.IntegrationTest

open Fable.Import.Jest.Matchers
open IML.IntegrationTest.StatefulPromise
open IML.IntegrationTest.VagrantTestFramework
open Fable.Import.Node.PowerPack
open Fable.Import.Node

type ExecOk = Stdout * Stderr
type ExecErr = ChildProcess.ExecError * Stdout * Stderr
type CommandFn = unit -> Fable.Import.JS.Promise<Result<ExecOk, ExecErr>>

let command = StatefulPromise()

let rb1 () =
  printfn "Invoking rb1"
  ChildProcess.exec (shellCommand "echo rollback command 1") None

let rb2 () =
  printfn "Invoking rb2"
  ChildProcess.exec (shellCommand "echo rollback command 2") None

let rb3 () =
  printfn "Invoking rb3"
  ChildProcess.exec (shellCommand "echo rollback command 3") None

let rb4 () =
  printfn "Invoking rb4"
  ChildProcess.exec (shellCommand "echo rollback command 4") None

testList "Simple Pass" [
  let withSetup f () =
    let data = "{ \"ACTION\": \"info\" }";
    command {
      let! (Stdout(x), _) = runTestCommand "echo hello" (Some(rb1))
      printfn "output is: %A" x
      let! (Stdout(x), _) = runTestCommand "echo goodbye" (Some(rb2))
      printfn "output is: %A" x
      let! (Stdout(x), _) = runTestCommand "udevadm trigger" (Some(rb3))
      printfn "udevadm output: %A" x
      //let! (Stdout(x), _) = vagrantRunCommand "sdfg" (Some(rb4))
      let! (Stdout(x), _) = vagrantPipeToShellCommand (sprintf "echo '%s'" data) ("socat - UNIX-CONNECT:/var/run/device-scanner.sock") (Some(rb4))
      f x
    } |> testRun []

  yield! testFixtureAsync withSetup [
    "should call end", fun x ->
      printfn "x: %A" x
  ]
]
