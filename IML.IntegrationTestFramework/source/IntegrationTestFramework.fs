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
open System.Text.RegularExpressions

type RollbackResult = Result<(string * Out), (string * Err)>
type RollbackState = RollbackResult list
type RollbackCommandState = JS.Promise<Result<Out * RollbackState, Err * RollbackState>>
type RollbackCommand = RollbackState -> RollbackCommandState
type State = RollbackState * RollbackCommand list
type CommandResult<'a, 'b> = Result<'a * State, 'b * State>
type RollbackStateResult<'a, 'b> = Result<'a * RollbackState, 'b * RollbackState>
type CommandResponseResult = Result<string * string * string, string * string * string>

let shellCommand: string -> string =
  sprintf "ssh -o LogLevel=error device-scanner.local '%s'"

let execShell (x:string): ChildProcessPromiseResult =
  ChildProcess.exec (shellCommand x) None

let cmd (x:string) ((logs, rollbacks):State):JS.Promise<CommandResult<Out, Err>> =
  execShell x
    |> Promise.map (function
      | Ok r -> Ok(r, (logs @ [Ok (x, r)], rollbacks))
      | Error e -> Error(e, (logs @ [Error (x, e)], rollbacks))
    )

let rbCmd (cmd:string) (rollbackState:RollbackState): RollbackCommandState =
  execShell cmd
    |> Promise.map(function
      | Ok out -> Ok(out, rollbackState @ [Ok (cmd, out)])
      | Error err -> Error(err, rollbackState @ [Error (cmd, err)])
    )

let removeKnownHostWarning: string -> string = function
  | x when Regex.Match(x, @"Warning: Permanently added '\d+\.\d+\.\d+\.\d+' \(ECDSA\) to the list of known hosts\.").Success -> ""
  | x -> x

let removeKnownHostFromRollbackResult: RollbackResult -> RollbackResult = function
  | Ok (cmd, (stdout, Stderr(stderr))) -> Ok (cmd, (stdout, Stderr(removeKnownHostWarning stderr)))
  | Error (cmd, (err, stdout, Stderr(stderr))) -> Error (cmd, (err, stdout, Stderr(removeKnownHostWarning stderr)))

let removeKnownHostWarningFromCommandState: (StatefulResult<State, Out, Err> * StatefulResult<RollbackState, Out, Err>) -> StatefulResult<State, Out, Err> =
  fst
    >> function
      | Ok (x, (rollbackState, commands)) -> Ok(x, (rollbackState |> List.map(removeKnownHostFromRollbackResult), commands))
      | Error (x, (rollbackState, commands)) -> Error(x, (rollbackState |> List.map(removeKnownHostFromRollbackResult), commands))

let removeKnownHostWarningFromRollbackState: (StatefulResult<State, Out, Err> * StatefulResult<RollbackState, Out, Err>) -> StatefulResult<RollbackState, Out, Err> =
  snd
    >> function
      | Ok (x, rollbackState) -> Ok(x, rollbackState |> List.map(removeKnownHostFromRollbackResult))
      | Error (x, rollbackState) -> Error(x, rollbackState |> List.map(removeKnownHostFromRollbackResult))

let cleanState (s:(StatefulResult<State, Out, Err> * StatefulResult<RollbackState, Out, Err>)): (StatefulResult<State, Out, Err> * StatefulResult<RollbackState, Out, Err>) =
  ((removeKnownHostWarningFromCommandState s), (removeKnownHostWarningFromRollbackState s))

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

let rollbackError (rb:RollbackCommand) (p:JS.Promise<CommandResult<Out, Err>>) : JS.Promise<CommandResult<Out, Err>> =
  p |>
    Promise.map(function
      | Ok (x, (logs, rollbacks)) -> Ok (x, (logs, rollbacks))
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

let mapResultToResultString: RollbackResult list -> CommandResponseResult list = List.map rollbackResultToResultString
let mapResultToString: RollbackResult list -> (string * string * string) list = List.map (rollbackResultToString)

let private removeNewlineFromEnd (s:string): string =
  match s.EndsWith("\n") with
    | true -> s.Remove (s.Length - 1)
    | false -> s

let stdErrText (s:string): string =
  let str = sprintf "Stderr: %s" s
  match s with
    | "" -> str
    | _ -> sprintf "\x1b[31m%s\x1b[0m" str

let stdOutText (s:string): string =
  sprintf "Stdout: %s" s

let writeStdoutMsg (msgFn: string -> string -> string): string list -> unit = (List.map removeNewlineFromEnd) >> (List.reduce msgFn) >> Globals.``process``.stdout.write >> ignore

let writeStderrMsg (msgFn: string -> string -> string): string list -> unit = (List.map removeNewlineFromEnd) >> (List.reduce msgFn) >> Globals.``process``.stderr.write >> ignore

let logCommands (title:string): (_ * StatefulResult<RollbackState, Out, Err>) -> unit =
  snd
    >> (function
      | Ok (_, logs) | Error (_, logs) ->
        Globals.``process``.stdout.write (sprintf "-------------------------------------------------
  Test logs for: %s
-------------------------------------------------\n" title) |> ignore
        logs |> mapResultToResultString |> List.iter (function
          | Error (cmd, stdout, stderr) ->
            [stdOutText stdout; stdErrText stderr]
              |> writeStdoutMsg (sprintf "Command Error: %s \n    %s\n    %s\n\n" cmd)

          | Ok (cmd, stdout, stderr) ->
            [stdOutText stdout; stdErrText stderr]
              |> writeStdoutMsg (sprintf "Command: %s \n    %s\n    %s\n\n" cmd)
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
  match rollbacks |> (not << List.isEmpty) with
    | true ->
      rollbacks
        |> List.reduce(fun r1 r2 ->
          (fun _ -> r2) >>= r1
        )
        |> run logs
    | _ ->
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
