// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Connections

open Fable.Core.JsInterop
open Fable.Import.Node
open Handlers
open IML.Types.CommandTypes

open Fable.Import.Node.PowerPack.Stream

type Connection =
  | Stream of Net.Socket
  | ReadOnly of Net.Socket

let mutable conns:Connection list = []

let addConn c =
  conns <- c :: conns

let removeConn c =
  conns <- List.filter ((<>) c) conns

let createConn c = function
    | Command.Stream ->
      Readable.onEnd (fun () -> removeConn (Stream c)) c
        |> ignore
      addConn (Stream c)
    | _ ->
      addConn (ReadOnly c)

let private removeDestroyed = function
  | ReadOnly _ -> true
  | Stream c -> not (!!c?destroyed)

let toBuffer x =
    x
    |> fun x -> x + "\n"
    |> buffer.Buffer.from

let private writeOrEnd (d:State) = function
  | Stream c ->
    d
      |> State.encoder
      |> toBuffer
      |> c.write
      |> ignore
  | ReadOnly c ->
    removeConn (ReadOnly c)
    c.``end`` ()

let writeConns (x:State) =
  conns <- List.filter removeDestroyed conns

  conns
    |> List.iter (writeOrEnd x)
