// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec DeviceScannerDaemon.Server

open Fable.Core
open Fable.Import.JS
open Fable.Import.Node.Net
open Fable.Core.JsInterop
open LineDelimitedJsonStream.Stream
open DeviceScannerDaemon.Handlers
open UdevEventTypes.EventTypes

let serverHandler (c:net_types.Socket) =
  c
    .pipe(getJsonStream<Events>())
    .on("error", fun (e:Error) -> printfn "Unable to parse message %s" e.message)
    .on("data", (dataHandler c)) |> ignore

let opts = createEmpty<net_types.CreateServerOptions>
opts.allowHalfOpen <- Some true

let private server = net.createServer(opts, serverHandler)

server.on("error", raise) |> ignore

let fd = createEmpty<net_types.Fd>
fd.fd <- 3

server.listen(fd) |> ignore
