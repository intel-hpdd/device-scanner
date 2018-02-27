// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Server

open Fable.Import.Node
open PowerPack.Stream
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
      Error ex
let serverHandler (c:Net.Socket):unit =
  c
    |> LineDelimited.create()
    |> map parser
    |> Readable.onError (fun (e:exn) ->
      eprintfn "Unable to parse message %s" e.Message
      c.``end``()
    )
    |> tap (function
      | Info -> Connections.addConn (Connections.Persistent c)
      | _ -> Connections.addConn (Connections.Ephemeral c)
    )
    |> map backCompatHandler
    |> Readable.onError raise
    |> map (
      toJson
        >> fun x -> x + "\n"
        >> buffer.Buffer.from
        >> Ok
    )
    |> iter Connections.writeConns
    |> ignore

let private fd = createEmpty<Net.Fd>
fd.fd <- 3

let opts = createEmpty<Net.CreateServerOptions>
opts.allowHalfOpen <- Some true

net
  .createServer(opts, serverHandler)
  .listen(fd)
    |> Readable.onError raise
    |> ignore
