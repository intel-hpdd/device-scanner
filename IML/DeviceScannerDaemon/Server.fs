// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Server

open Fable.Import.Node
open Fable.Import.Node.PowerPack.Stream
open Fable.Import.JS
open Fable.Core.JsInterop
open IML.DeviceScannerDaemon.Handlers


let mutable conns = []

let serverHandler (c:Net.Socket):unit =
  conns <- c :: conns

  let removeConn () =
    Fable.Import.JS.console.error(List.length conns)

    List.filter (fun x -> c = x) conns

  let writeConns x =
    List.filter (fun (x:Net.Socket) -> not (!!x?destroyed)) conns
      |> List.iter (fun c -> Writable.write x c |> ignore)

  c
    |> Readable.onEnd (fun () -> conns <- removeConn())
    |> LineDelimitedJson.create()
    |> Readable.onError (fun (e:Error) ->
      console.error ("Unable to parse message " + e.message)
      conns <- removeConn()
      c.``end``()
    )
    |> map dataHandler
    |> map (toJson >> buffer.Buffer.from >> Ok)
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
