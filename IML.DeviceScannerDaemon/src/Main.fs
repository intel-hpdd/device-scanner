// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Server

open Fable.Import.Node
open Fable.Import.Node.PowerPack.Stream
open Fable.Import
open Fable.Core.JsInterop
open Handlers
open IML.Types.CommandTypes

let private parser x =
  try
    x
      |> ofJson<Command>
      |> Ok
  with
    | ex ->
      Error (!!ex)

let serverHandler (c:Net.Socket):unit =
  Connections.addConn c

  c
    |> Readable.onEnd (Connections.removeConn c)
    |> LineDelimited.create()
    |> map parser
    |> Readable.onError (fun (e:JS.Error) ->
      eprintfn "Unable to parse message %s" e.message
    )
    |> map (
        handler
        >> toJson
        >> fun x -> x + "\n"
        >> buffer.Buffer.from
        >> Ok
    )
    |> iter Connections.writeConns
    |> ignore

let private fd = createEmpty<Net.Fd>
fd.fd <- 3

net
  .createServer(serverHandler)
  .listen(fd)
    |> Readable.onError raise
    |> ignore