// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec DeviceScannerDaemon.Server

open Node.Net
open Fable.Core
open LineDelimitedJsonStream.Stream
open DeviceScannerDaemon.Handlers

let serverHandler (c:net_types.Socket) =
  c
    .pipe(getJsonStream())
    .on("data", (dataHandler c)) |> ignore

let private server = net.createServer serverHandler

server.on("error", raise) |> ignore

[<Pojo>]
type Rec = { fd: int; }
server.listen({ fd = 3; }) |> ignore
