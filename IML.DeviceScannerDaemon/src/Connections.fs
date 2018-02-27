// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Connections

open Fable.Core.JsInterop
open Fable.Import.Node
open Fable.Import.Node.PowerPack.Stream

/// Due to backcompat with Python sockets,
/// We need to support half open connections.
/// Because of this, we need to model persistent
/// and ephemeral connections.
type Connection =
  | Persistent of Net.Socket
  | Ephemeral of Net.Socket

let mutable conns:Connection list = []

let removeConn (c) () =
  conns <- List.filter ((<>) c) conns

let addConn = function
  | Persistent c ->
    Readable.onEnd (removeConn (Persistent c)) c
      |> ignore
    conns <- Persistent c :: conns
  | Ephemeral c ->
    conns <- Ephemeral c :: conns

let private removeDestroyed = function
  | Ephemeral _ -> true
  | Persistent c -> not (!!c?destroyed)

let private writeOrEnd x = function
  | Ephemeral c ->
    removeConn (Ephemeral c) ()
    Writable.``end`` (Some x) c
  | Persistent c ->
    Writable.write x c
      |> ignore

let writeConns x =
  conns <- List.filter removeDestroyed conns

  conns
    |> List.iter (writeOrEnd x)
