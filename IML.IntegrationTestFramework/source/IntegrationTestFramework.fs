// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTestFramework.IntegrationTestFramework

open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open Fable.PowerPack
open IML.StatefulPromise.StatefulPromise

type RollbackState = Result<Out, Err> list
type RollbackCommandState = JS.Promise<Result<Out * RollbackState, Err * RollbackState>>
type RollbackCommand = RollbackState -> RollbackCommandState
type State = Result<Out, Err> list * RollbackCommand list
type CommandResult<'a, 'b> = Result<'a * State, 'b * State>

let shellCommand =
  sprintf "ssh devicescannernode '%s'"

let execShell x =
  ChildProcess.exec (shellCommand x) None

let cmd (x:string) ((logs, rollbacks):State):JS.Promise<CommandResult<Out, Err>> =
  execShell x
    |> Promise.map (function
      | Ok x -> Ok(x, (logs @ [Ok x], rollbacks))
      | Error e -> Error(e, (logs @ [Error e], rollbacks))
    )
let addToRollbackState (rollbackState:RollbackState) : (JS.Promise<Result<Out, Err>> -> JS.Promise<Result<Out * RollbackState, Err * RollbackState>>) =
  Promise.map(function
    | Ok out -> Ok(out, rollbackState @ [Ok out])
    | Error err -> Error(err, rollbackState @ [Error err])
  )
let pipeToShellCmd (leftCmd:string) (rightCmd:string) ((logs, rollbacks):State):JS.Promise<CommandResult<Out, Err>> =
  ChildProcess.exec (sprintf "%s | %s" leftCmd (shellCommand rightCmd)) None
    |> Promise.map (function
      | Ok x -> Ok(x, (logs @ [Ok x], rollbacks))
      | Error e -> Error(e, (logs @ [Error e], rollbacks))
    )

let ignoreCmd : (JS.Promise<CommandResult<Out, Err>> -> JS.Promise<Result<unit * State, Err * State>>) =
  Promise.map (function
    | Ok (_, s) -> Ok((), s)
    | Error (e, s) -> Error(e, s)
  )

let rollback (rb:RollbackCommand) (p:JS.Promise<CommandResult<Out, Err>>) : JS.Promise<CommandResult<Out, Err>> =
  p |>
    Promise.map(function
      | Ok (x, (logs, rollbacks)) -> Ok (x, (logs, rb :: rollbacks))
      | Error (e, (logs, rollbacks)) -> Error (e, (logs, rb :: rollbacks))
    )

let private logCommands (results:Result<Out, Err> list) : string list =
  results
    |> List.map (function
      | Error (e, _, _) ->
        let msg = sprintf "/%A" !!e
        eprintfn "%A" msg
        msg
      | Ok x ->
        let msg = sprintf "%A" x
        printfn "%A" msg
        msg
    )

let private getState<'a, 'b> (fn: 'a -> 'b) : (Result<Out * 'a, Err * 'a> -> 'b) = function
  | Ok(_, s) ->
    fn s
  | Error(_, s) ->
    fn s

let private runTeardown ((logs, rollbacks):State): StatefulPromiseResult<RollbackState, Out, Err>=
  if not(List.isEmpty rollbacks) then
    rollbacks
      |> List.reduce(fun r1 r2 ->
        (fun _ -> r2) >>= r1
      )
      |> run logs
  else
    Promise.lift(Ok((Stdout(""), Stderr("")), logs))

let run (state:State) (fn:StateS<State, Out, Err>): JS.Promise<string list> =
  run state fn
    |> Promise.bind (getState runTeardown)
    |> Promise.map (getState logCommands)

let startCommand (p:StateS<State, Out, Err>): JS.Promise<string list> =
  p |> run ([], [])
