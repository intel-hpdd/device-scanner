// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Server

open Fable.Core.JsInterop
open Fable.Import.Node
open Handlers

let private server = http.createServer (serverHandler)
let private fd = createEmpty<Net.Fd>

fd.fd <- 3
server.listen (fd) |> ignore
