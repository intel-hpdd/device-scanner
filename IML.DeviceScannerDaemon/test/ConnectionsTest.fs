// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.ConnectionsTest

open Fable.Import.Node
open Fable.Import.Jest
open Matchers


test "adding a connection" <| fun () ->
  let c = Connections.Persistent (net.Socket.Create())

  Connections.addConn c

  Connections.conns == [c]

  Connections.removeConn c ()

test "removing a connection" <| fun () ->
  let c = Connections.Ephemeral (net.Socket.Create())

  Connections.addConn c

  Connections.removeConn c ()

  Connections.conns == []


testDone "writing a connection" <| fun (d) ->
  expect.assertions 2

  let server =
    net.createServer(fun c ->
      Connections.addConn (Connections.Persistent c)

      Connections.writeConns (buffer.Buffer.from "foo")
    )

  server.listen(fun () ->
    let address = (server.address() :?> string)

    let sock = net.createConnection address

    sock.once("data", fun (x) ->
      x == buffer.Buffer.from "foo"

      sock.``end``()

      server.close(fun _ ->
        Connections.conns == []

        d.``done``()
      )
        |> ignore
    )
      |> ignore

  )
    |> ignore
