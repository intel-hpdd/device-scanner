// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.DeviceScannerDaemon.ConnectionsTest

open Fable.Import.Node
open Fable.Import.Jest
open Matchers


test "adding a connection" <| fun () ->
  let s = net.Socket.Create()

  Connections.addConn s

  Connections.conns == [s]

  Connections.removeConn s ()

test "removing a connection" <| fun () ->
  let s = net.Socket.Create()

  Connections.addConn s

  Connections.removeConn s ()

  Connections.conns == []


testDone "writing a connection" <| fun (d) ->
  expect.assertions 1

  let server = 
    net.createServer(fun (s) ->
      Connections.addConn s

      Connections.writeConns (buffer.Buffer.from "foo")

      Connections.removeConn s ()
    )

  server.listen(fun () ->
    let address = (server.address() :?> string)

    let sock = net.createConnection address

    sock.once("data", fun (x) -> 
      x == buffer.Buffer.from "foo"
      
      sock.``end``()

      server.close()
        |> ignore
      
      d.``done``()
    )
      |> ignore

  )
    |> ignore
