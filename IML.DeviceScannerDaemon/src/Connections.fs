// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Connections

open Fable.Core.JsInterop
open Fable.Import.Node
open Fable.Import.Node.PowerPack.Stream

let mutable conns:Net.Socket list = []

let addConn c =
  conns <- c :: conns

let removeConn (c) () =
  conns <- List.filter ((<>) c) conns

let writeConns x =
  conns <- List.filter (fun (v:Net.Socket) -> not (!!v?destroyed)) conns

  conns
    |> List.iter (fun c -> Writable.write x c |> ignore)