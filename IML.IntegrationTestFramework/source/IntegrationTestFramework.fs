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

type RollbackResult = Result<(string * Out), (string * Err)>
type RollbackState = RollbackResult list
type RollbackCommandState = JS.Promise<Result<Out * RollbackState, Err * RollbackState>>
type RollbackCommand = RollbackState -> RollbackCommandState
type State = RollbackState * RollbackCommand list
type CommandResult<'a, 'b> = Result<'a * State, 'b * State>
type RollbackResult<'a, 'b> = Result<'a * RollbackState, 'b * RollbackState>
type CommandResponseResult = Result<string * string * string, string * string * string>

let shellCommand =
  sprintf "ssh devicescannernode '%s'"

let execShell x =
  ChildProcess.exec (shellCommand x) None

let cmd (x:string) ((logs, rollbacks):State):JS.Promise<CommandResult<Out, Err>> =
  execShell x
    |> Promise.map (function
      | Ok r -> Ok(r, (logs @ [Ok (x, r)], rollbacks))
      | Error e -> Error(e, (logs @ [Error (x, e)], rollbacks))
    )
let addToRollbackState (cmd:string) (rollbackState:RollbackState) : (ChildProcessPromiseResult -> JS.Promise<RollbackResult<Out, Err>>) =
  Promise.map(function
    | Ok out ->
      Ok(out, rollbackState @ [Ok (cmd, out)])
    | Error err -> Error(err, rollbackState @ [Error (cmd, err)])
  )
let pipeToShellCmd (leftCmd:string) (rightCmd:string) ((logs, rollbacks):State):JS.Promise<CommandResult<Out, Err>> =
  let cmd = sprintf "%s | %s" leftCmd (shellCommand rightCmd)
  ChildProcess.exec cmd None
    |> Promise.map (function
      | Ok x -> Ok(x, (logs @ [Ok (cmd, x)], rollbacks))
      | Error e -> Error(e, (logs @ [Error (cmd, e)], rollbacks))
    )

let ignoreCmd : (JS.Promise<CommandResult<Out, Err>> -> JS.Promise<CommandResult<unit, Err>>) =
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

let errToString ((execError:ChildProcess.ExecError), _, Stderr(y)): string =
  sprintf "%A - %A" !!execError y

let rollbackResultToString: (RollbackResult -> string * string * string) = function
  | Ok ((cmd:string), (Stdout(x), Stderr(y))) -> (cmd, x, y)
  | Error ((cmd:string), (_, Stdout(x), Stderr(y))) -> (cmd, x, y)

let rollbackResultToResultString: (RollbackResult -> CommandResponseResult) = function
  | Ok ((cmd:string), (Stdout(x), Stderr(y))) -> Ok(cmd, x, y)
  | Error ((cmd:string), (_, Stdout(x), Stderr(y))) -> Error(cmd, x, y)

let mapRollbackResultToResultString: RollbackResult list -> CommandResponseResult list = List.map rollbackResultToResultString
let mapRollbackResultToString: RollbackResult list -> (string * string * string) list = List.map (rollbackResultToString)

let private removeNewlineFromEnd (s:string): string =
  if s.EndsWith("\n") then
    s.Remove (s.Length - 1)
  else
    s
let writeStdoutMsg (msgFn: string -> string -> string): string list -> unit = (List.map removeNewlineFromEnd) >> (List.reduce msgFn) >> Globals.process.stdout.write >> ignore
let writeStderrMsg (msgFn: string -> string -> string): string list -> unit = (List.map removeNewlineFromEnd) >> (List.reduce msgFn) >> Globals.process.stderr.write >> ignore

let logCommands (title:string): (_ * StatefulResult<RollbackState, Out, Err>) -> unit =
  snd
    >> (function
      | Ok (_, logs) | Error (_, logs) ->
        Globals.process.stdout.write (sprintf "-------------------------------------------------
  Test logs for: %s
-------------------------------------------------\n" title) |> ignore
        logs |> mapRollbackResultToResultString |> List.iter (function
          | Error (cmd, stdout, stderr) ->
            [stdout; stderr]
              |> writeStdoutMsg (sprintf "Command Error: %s: \nStdout: %s\n Stderr: %s\n\n" cmd)
          | Ok (cmd, stdout, stderr) ->
            [stdout; stderr]
              |> writeStdoutMsg (sprintf "Command: %s \nStdout: %s\n Stderr: %s\n\n" cmd)
        ))

let private getState<'a, 'b> (fn: 'a -> 'b) : (Result<Out * 'a, Err * 'a> -> 'b) = function
  | Ok(_, s) ->
    fn s
  | Error(_, s) ->
    fn s

let private attachToPromise<'a, 'b> (r:'a): JS.Promise<'b> -> JS.Promise<'a * 'b> =
  Promise.map(fun x ->
    (r, x)
  )

let private runTeardown ((logs, rollbacks):State): StatefulPromiseResult<RollbackState, Out, Err> =
  if not(List.isEmpty rollbacks) then
    rollbacks
      |> List.reduce(fun r1 r2 ->
        (fun _ -> r2) >>= r1
      )
      |> run logs
  else
    Promise.lift(Ok((Stdout(""), Stderr("")), logs))

let run (state:State) (fn:StateS<State, Out, Err>): (JS.Promise<StatefulResult<State, Out, Err> * StatefulResult<RollbackState, Out, Err>>) =
  run state fn
    |> Promise.bind (fun r ->
      r
        |> ((getState runTeardown) >> (attachToPromise r))
    )

let startCommand (title:string) (fn:StateS<State, Out, Err>): (JS.Promise<StatefulResult<State, Out, Err> * StatefulResult<RollbackState, Out, Err>>) =
  fn |> run ([], [])
    |> Promise.tap (logCommands title)
