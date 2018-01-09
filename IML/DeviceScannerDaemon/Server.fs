// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Server

open Fable.Import.Node
open Fable.Import.Node.PowerPack.Stream
open Fable.Import
open Fable.Core.JsInterop
open IML.DeviceScannerDaemon.Handlers

let counterFactory () =
  let mutable count = 1

  fun () ->
    count <- count + 1
    count

let counter = counterFactory()

let mutable conns = Map.empty<int, Net.Socket>

let removeConn (i) () =
    conns <- Map.filter (fun k _ -> k <> i) conns

let writeConns x =
  conns <- Map.filter (fun _ (v:Net.Socket) -> not (!!v?destroyed)) conns

  conns
    |> Map.iter (fun _ c -> Writable.write x c |> ignore)

let serverHandler (c:Net.Socket):unit =
  let index = counter()

  conns <- Map.add index c conns

  let remove = removeConn index
  c
    |> Readable.onEnd (remove)
    |> LineDelimitedJson.create()
    |> Readable.onError (fun (e:JS.Error) ->
      JS.console.error ("Unable to parse message " + e.message)

      remove()
      c.``end``()
    )
    |> map dataHandler
    |> map (
      toJson
        >> (+) "\n"
        >> buffer.Buffer.from
        >> Ok
    )
    |> iter writeConns
    |> ignore

let private server = net.createServer(serverHandler)

server
  |> Readable.onError raise
  |> ignore

let private fd = createEmpty<Net.Fd>
fd.fd <- 3

server.listen(fd)
  |> ignore
