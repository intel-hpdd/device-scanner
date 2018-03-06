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
  | V1 of Net.Socket
  | V2 of Net.Socket
  | ReadOnly of Net.Socket

let mutable conns:Connection list = []

let addConn c =
  conns <- c :: conns

let removeConn (c) =
  conns <- List.filter ((<>) c) conns

let createConn c = function
    | Info ->
      Readable.onEnd (fun () -> removeConn (V2 c)) c
        |> ignore
      addConn (V2 c)
    | ACTION _ ->
      addConn (V1 c)
    | _ ->
      addConn (ReadOnly c)

let private removeDestroyed = function
  | V1 _ | ReadOnly _ -> true
  | V2 c -> not (!!c?destroyed)

let toBuffer x =
    x
    |> toJson
    |> fun x -> x + "\n"
    |> buffer.Buffer.from

let private writeOrEnd (d:Data) = function
  | V2 c ->
    c.write (toBuffer d)
      |> ignore
  | V1 c ->
    let x =
      d.blockDevices
        |> toBuffer

    removeConn (ReadOnly c)
    c.``end`` x
  | ReadOnly c ->
    removeConn (ReadOnly c)
    c.``end`` ()

let writeConns (x:Data) =
  conns <- List.filter removeDestroyed conns

  conns
    |> List.iter (writeOrEnd x)
