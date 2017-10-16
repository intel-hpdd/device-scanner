module IML.Test.VagrantTest

open Fable.PowerPack
open Fable.Import
open Fable.Core.JsInterop
open Fable.Import.Node
open Fable.Core

type Out = {
  err: ChildProcess.ExecError option;
  stdout: U2<string, Buffer.Buffer>;
  stderr: U2<string, Buffer.Buffer>
}

let private opts = createEmpty<ChildProcess.ExecOptions>

let private execHandler (cmd:string) =
  Promise.create(fun res rej ->
    ChildProcess.exec(cmd, opts, (fun e stdout stderr ->
      match e with
        | Some (e) ->
          JS.console.error(stderr)
          rej(!!e)
        | None ->
          match stdout with
            | U2.Case1(x) -> res x
            | U2.Case2(x) -> res(x.toString "utf8")
    ))
      |> ignore
  )

let private vagrantCommand (cmd:string) =
  sprintf "vagrant %s" cmd

let private shellCommand (cmd:string) =
  sprintf "vagrant sh -u root -c '%s' default" cmd

let vagrantStart () =
  execHandler (vagrantCommand "up")

let vagrantDestroy () =
  execHandler (vagrantCommand "destroy -f")

let vagrantRunCommand cmd =
  execHandler (shellCommand cmd)

let vagrantPipeToShellCommand (cmd1:string) (cmd2:string) =
  let cmd = sprintf "%s | %s" cmd1 (shellCommand cmd2)
  execHandler cmd
