module IML.Test.VagrantTestFramework

open Fable.Import.Node
open Fable.Core
open NodeHelpers

type Out = {
  err: ChildProcess.ExecError option;
  stdout: U2<string, Buffer.Buffer>;
  stderr: U2<string, Buffer.Buffer>
}

let private vagrantCommand (cmd:string) =
  sprintf "vagrant %s" cmd

let private shellCommand (cmd:string) =
  sprintf "vagrant ssh default -- '%s'" cmd

let vagrantStart () =
  exec (vagrantCommand "up")

let vagrantDestroy () =
  exec (vagrantCommand "destroy -f")

let vagrantRunCommand cmd () =
  exec (shellCommand cmd)

let vagrantPipeToShellCommand (cmd1:string) (cmd2:string) () =
  let cmd = sprintf "%s | %s" cmd1 (shellCommand cmd2)
  exec cmd
