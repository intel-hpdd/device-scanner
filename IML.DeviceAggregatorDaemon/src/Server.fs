// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.Server

open Fable.Core.JsInterop
open Fable.Import.Node
open Handlers
open IML.CommonLibrary
let private server = http.createServer serverHandler

let private isSystemd: bool =
  !!Globals.``process``.env?LISTEN_PID
    |> Option.filter (fun x -> x > 0)
    |> Option.isSome

(
if isSystemd then
  let fd = createEmpty<Net.Fd>
  fd.fd <- 3

  server.listen fd
else
  let port: int option =
    !!Globals.``process``.env?AGGREGATOR_PORT
    |> Option.expect "If device-aggregator is not running in systemd, the AGGREGATOR_PORT environment variable is required."

  server.listen port
) |> ignore
