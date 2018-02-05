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

type PromiseResultS = unit -> ChildProcess.ChildProcessPromiseResult
type CommandResult<'a, 'b> = Result<'a * PromiseResultS list, 'b * PromiseResultS list>

let shellCommand (cmd:string) =
  sprintf "ssh devicescannernode '%s'" cmd

let execShell x =
  ChildProcess.exec (shellCommand x) None

let cmd (x:string) (s:PromiseResultS list):JS.Promise<CommandResult<Out, Err>> =
  execShell x
    |> Promise.map (function
      | Ok x -> Ok(x, s)
      | Error x -> Error(x, s)
    )

let ignoreCmd p =
  p
    |> Promise.map (function
      | Ok (_, s) -> Ok((), s)
      | Error (e, s) -> Error(e, s)
    )

let rollback (rb:PromiseResultS) (p:JS.Promise<CommandResult<'a, 'b>>):JS.Promise<CommandResult<'a, 'b>> =
  p
    |> Promise.map(function
      | Ok (x, s) -> Ok (x, rb :: s)
      | Error (e, s) -> Error (e, rb :: s)
    )

let private runTeardown (errorList:PromiseResultS list) =
  errorList
    |> List.fold (fun acc rb ->
      acc
        |> Promise.bind(fun _ -> rb())
    ) (Promise.lift(Ok(Stdout(""), Stderr(""))))
    |> Promise.map (ignore)

let run state fn =
  promise {
    let! runResult = run state fn

    match runResult with
      | Ok(result, rollbacks) ->
        do! runTeardown(rollbacks)
        return result
      | Error((e, _, _), rollbacks) ->
        do! runTeardown(rollbacks)
        return! raise !!e
  }
