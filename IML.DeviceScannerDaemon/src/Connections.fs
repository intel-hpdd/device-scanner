// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Connections

open Fable.Core.JsInterop
open Fable.Import.Node
open IML.Types.ScannerStateTypes
open IML.Types.CommandTypes
open Fable.Import.Node.PowerPack.Stream

type Connection =
    | Stream of Net.Socket
    | ReadOnly of Net.Socket

let mutable conns : Connection list = []
let addConn c = conns <- c :: conns
let removeConn c = conns <- List.filter ((<>) c) conns

let createConn connection command =
    let conn =
        match command with
        | Command.Stream ->
            let conn = Stream connection
            Readable.onEnd (fun () ->
                connection.``end``()
                removeConn conn) connection
            |> ignore
            conn
        | _ -> (ReadOnly connection)
    addConn conn
    connection.once ("error",
                     fun (e : exn) ->
                         removeConn conn
                         eprintfn "Unexpected socket error: %s " e.Message
                         eprintfn "trace: %s " e.StackTrace)

let private removeDestroyed =
    function
    | ReadOnly _ -> true
    | Stream c -> not (!!c?destroyed)

let toBuffer x =
    x
    |> fun x -> x + "\n"
    |> buffer.Buffer.from

let private writeOrEnd (d : State) =
    function
    | Stream c ->
        d
        |> State.encoder
        |> toBuffer
        |> c.write
        |> ignore
    | ReadOnly c ->
        removeConn (ReadOnly c)
        c.``end``()

let writeConns (x : State) =
    conns <- List.filter removeDestroyed conns
    conns |> List.iter (writeOrEnd x)
