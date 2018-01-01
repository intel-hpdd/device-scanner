// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module NodeHelpers

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

    let ``end`` (c:Net.Socket) = function
      | Some(x) -> c.``end``(x)
      | None -> c.``end``()

    let onceConnect (fn:unit -> unit) (c:Net.Socket)  = c.once("connect", fn) :?> Net.Socket

    let onConnect (fn:unit -> unit) (c:Net.Socket)  = c.on("connect", fn) :?> Net.Socket

    let onData (fn:string -> unit) (c:Stream.Stream) = c.on("data", fn) :?> Net.Socket

    let onError (fn:string -> unit) (c:Stream.Stream) = c.on("error", fn) :?> Net.Socket

    let connect (x:NetPath) = Net.connect x

    let createServer opts serverHandler = Net.createServer(opts, serverHandler)

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

        ChildProcess.exec(cmd, opts, (fun (e, stdout', stderr') ->
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
