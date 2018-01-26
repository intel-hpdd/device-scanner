// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTest.VagrantTestFramework

open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open Fable.PowerPack

open IML.IntegrationTest.StatefulPromise

type PromiseResultS = unit -> ChildProcessPromiseResult
type CommandResult = Result<Out * PromiseResultS list, Err * PromiseResultS list>

let vagrantCommand (cmd:string) =
  sprintf "vagrant %s" cmd

let shellCommand (cmd:string) =
  sprintf "vagrant ssh default -- '%s'" cmd

let private mapChildProcessPromise rollback s p : JS.Promise<CommandResult> =
  p
    |> Promise.map (function
      | Ok (x) ->
        match rollback with
          | Some(rb) ->
            Ok(x, rb :: s)
          | None -> Ok(x, s)
      | Error (x) ->
         match rollback with
           | Some(rb) ->
              Error(x, rb :: s)
           | None -> Error(x, s)
    )

let private execCommand cmd rb s : JS.Promise<CommandResult> =
    ChildProcess.exec (cmd) None
      |> (mapChildProcessPromise rb s)


let vagrantStart rb =
  execCommand (vagrantCommand "up") rb

let vagrantDestroy rb =
  execCommand (vagrantCommand "destroy -f") rb

let vagrantRunCommand cmd rb =
  execCommand (shellCommand cmd) rb

let vagrantPipeToShellCommand cmd1 cmd2 rb =
  execCommand (sprintf "%s | %s" cmd1 (shellCommand cmd2)) rb

let runTeardown (errorList:PromiseResultS list) =
  errorList
    |> List.fold (fun acc rb ->
      acc
        |> Promise.bind(fun x ->
          printfn "x is: %A" x
          rb()
        )
      ) (Promise.lift(Ok(Stdout(""), Stderr(""))))

let testRun state fn =
  promise {
    let! runResult = run state fn
    match runResult with
      | Ok(result, rollbacks) ->
        let! _ = runTeardown(rollbacks)
        return result
      | Error((e, _, _), rollbacks) ->
        let! _ = runTeardown(rollbacks)
        return! raise !!e
  }
