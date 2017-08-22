// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec IML.DeviceScannerDaemon.Server

open Fable.Import.Node
open Fable.Core
open Fable.Import.JS
open Fable.Core.JsInterop
open IML.LineDelimitedJsonStream.Stream
open IML.DeviceScannerDaemon.Handlers
open IML.UdevEventTypes.EventTypes

let serverHandler (c:Net.Socket) =
  c
    .pipe(getJsonStream<Events>())
    .on("error", fun (e:Error) -> printfn "Unable to parse message %s" e.message)
    .on("data", (dataHandler c)) |> ignore

let opts = createEmpty<Net.CreateServerOptions>
opts.allowHalfOpen <- Some true

let private server = Net.createServer(opts, serverHandler)

let r e =
  raise e
  |> ignore

server.on("error", r) |> ignore

let fd = createEmpty<Net.Fd>
fd.fd <- 3

server.listen(fd) |> ignore
