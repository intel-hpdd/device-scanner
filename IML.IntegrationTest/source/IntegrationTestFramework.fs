// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTest.IntegrationTestFramework

open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open Fable.PowerPack

open IML.StatefulMonad.StatefulPromise

type PromiseResultS = unit -> ChildProcessPromiseResult
type CommandResult = Result<Out * PromiseResultS list, Err * PromiseResultS list>

let shellCommand (cmd:string) =
  sprintf "ssh devicescannernode '%s'" cmd

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

let runTestCommand cmd rb =
  execCommand (shellCommand cmd) rb

let runTeardown (errorList:PromiseResultS list) =
  errorList
    |> List.fold (fun acc rb ->
      acc
        |> Promise.bind(fun _ -> rb())
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
