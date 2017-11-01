// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec NodeHelpers

open System
open Fable.Import.Node
open Fable.Core.JsInterop
open Fable.Core
open Fable.PowerPack


  [<AutoOpen>]
  module NetHelpers =
    [<Pojo>]
    type NetPath = {
      path: string
    }

    type ShellError = {
      cmd : string;
      code : int;
      msg : string;
      stdout : U2<string, Buffer.Buffer>;
      stderr : U2<string, Buffer.Buffer>;
      signal : string option;
      failed : bool
    }

    type ShellSuccess = {
      cmd : string;
      stdout : U2<string, Buffer.Buffer>;
      stderr : U2<string, Buffer.Buffer>;
      code : int;
      failed : bool
    }

    let ``end`` (c:Net.Socket) = function
      | Some(x) -> c.``end``(x)
      | None -> c.``end``()

    let onceConnect (fn:unit -> unit) (c:Net.Socket)  = c.once("connect", fn) :?> Net.Socket

    let onConnect (fn:unit -> unit) (c:Net.Socket)  = c.on("connect", fn) :?> Net.Socket

    let onData (fn:string -> unit) (c:Stream.Stream) = c.on("data", fn) :?> Net.Socket

    let onError (fn:string -> unit) (c:Stream.Stream) = c.on("error", fn) :?> Net.Socket

    let connect (x:NetPath) = Net.connect x

    let createServer opts serverHandler = Net.createServer(opts, serverHandler)

    let execFn cmd (res:Result<ShellSuccess, ShellError> -> unit) (errOpt:ChildProcess.ExecError option) stdout stderr =
        match errOpt with
            | None ->
                Ok ({cmd = cmd;
                            stdout = stdout;
                            stderr = stderr;
                            code = 0;
                            failed = false})
            | Some(err) ->
                Error ({cmd = cmd;
                        code = err.code;
                        msg = err.message;
                        signal = err.signal;
                        failed = true;
                        stdout = stdout;
                        stderr = stderr})
        |> res

    let shell(cmd : string) (args:ResizeArray<string> option) (opts: ChildProcess.ExecOptions option) =
      let opts:ChildProcess.ExecOptions =
        match opts with
          | None ->
             let o = createEmpty<ChildProcess.ExecOptions>
             o.encoding <- Some("utf8")
             o
          | Some(x) -> x

      Promise.create(
          fun res _ ->
              ChildProcess.exec(cmd, opts, (execFn cmd res)) |> ignore
      )

    let spawnFn (cmd:string) (args:ResizeArray<string> option) (opts: ChildProcess.ExecOptions option) =
      let args:ResizeArray<string> =
        match args with
          | None -> new ResizeArray<String>()
          | Some(x) -> x
      let opts:ChildProcess.ExecOptions =
        match opts with
          | None ->
            let o = createEmpty<ChildProcess.ExecOptions>
            o.encoding <- Some("utf8")
            o
          | Some(x) -> x


      ChildProcess.spawn(cmd, args, opts)


[<AutoOpen>]
  module ChildProcessHelpers =
    type Stdout = Stdout of string
    type Stderr = Stderr of string

    type ExecOk = Stdout * Stderr
    type ExecErr = ChildProcess.ExecError * Stdout * Stderr

    let private toStr = function
      | U2.Case1(x) -> x
      | U2.Case2(x:Buffer.Buffer) -> x.toString "utf8"

    let exec (cmd:string) =
      Promise.create(fun res _ ->

        let opts = createEmpty<ChildProcess.ExecOptions>

        ChildProcess.exec(cmd, opts, (fun e stdout' stderr' ->
          let stdout = stdout' |> toStr |> Stdout
          let stderr = stderr' |> toStr |> Stderr

          match e with
            | Some (e) ->
              (e, stdout, stderr)
                |> Error
                |> res
            | None ->
              (stdout, stderr)
                |> Ok
                |> res
        ))
          |> ignore
      )
