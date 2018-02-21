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

type CommandRollback = unit -> ChildProcess.ChildProcessPromiseResult
type State = Result<Out, Err> list * CommandRollback list
type CommandResult<'a, 'b> = Result<'a * State, 'b * State>

let shellCommand (cmd:string) =
  let newCommand = sprintf "ssh devicescannernode '%s'" cmd
  newCommand

let execShell x =
  ChildProcess.exec (shellCommand x) None

let cmd (x:string) ((logs, rollbacks):State):JS.Promise<CommandResult<Out, Err>> =
  execShell x
    |> Promise.map (function
      | Ok x -> Ok(x, (logs @ [Ok x], rollbacks))
      | Error e -> Error(e, (logs @ [Error e], rollbacks))
    )

let pipeToShellCmd (leftCmd:string) (rightCmd:string) ((logs, rollbacks):State):JS.Promise<CommandResult<Out, Err>> =
  ChildProcess.exec (sprintf "%s | %s" leftCmd (shellCommand rightCmd)) None
    |> Promise.map (function
      | Ok x -> Ok(x, (logs @ [Ok x], rollbacks))
      | Error e -> Error(e, (logs @ [Error e], rollbacks))
    )

let ignoreCmd p =
  p
    |> Promise.map (function
      | Ok (_, s) -> Ok((), s)
      | Error (e, s) -> Error(e, s)
    )

let rollback (rb:CommandRollback) (p:JS.Promise<CommandResult<'a, 'b>>):JS.Promise<CommandResult<'a, 'b>> =
  p
    |> Promise.map(function
      | Ok (x, (logs, rollbacks)) -> Ok (x, (logs, rb :: rollbacks))
      | Error (e, (logs, rollbacks)) -> Error (e, (logs, rb :: rollbacks))
    )

let private logCommands (results, _) =
  results
    |> List.iter (function
      | Error (e, _, _) -> eprintfn "%A" !!e
      | Ok x -> printfn "%A" x
    )

let private runTeardown ((logs, rollbacks):State) =
  let teardownLogs = []
  rollbacks
    |> List.fold (fun acc rb ->
      acc
        |> Promise.bind(function
          | Ok (stdout, _) ->

            rb()
          | Error e -> failwith (sprintf "Error rolling back: %A" !!e)
        )
    ) (Promise.lift(Ok(Stdout(""), Stderr(""))))
    |> Promise.map (function
      | Ok _ -> ()
      | Error e ->
        failwith (sprintf "Error rolling back: %A" !!e)
    )

let run state fn =
  promise {
    let! runResult = run state fn

    match runResult with
      | Ok(result, s) ->
        do! runTeardown(s)
        logCommands s
        return result
      | Error((e, _, _), s) ->
        do! runTeardown(s)
        logCommands s
        return! raise !!e
  }

let startCommand p =
  p |> run ([], [])
