// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.ConnectionsTest

open Fable.Import.Node
open Fable.Import.Jest
open IML.Types.CommandTypes
open IML.Types.ScannerStateTypes
open Matchers

test "adding a connection" <| fun () ->
    let c = Connections.Stream(net.Socket.Create())
    Connections.addConn c
    Connections.conns == [ c ]
    Connections.removeConn c
test "removing a connection" <| fun () ->
    let c = Connections.Stream(net.Socket.Create())
    Connections.addConn c
    Connections.removeConn c
    Connections.conns == []
testDone "writing a connection" <| fun d ->
    expect.assertions 2
    let server =
        net.createServer (fun c ->
            Connections.createConn c Stream |> ignore
            let d =
                { blockDevices = Map.empty
                  zed = Map.empty
                  localMounts = Set.empty }
            Connections.writeConns d)
    server.listen (fun () ->
        let address = (server.address() :?> string)
        let sock = net.createConnection address
        sock.once ("data",
                   fun x ->
                       toMatchSnapshot x
                       sock.``end``()
                       server.close (fun _ ->
                           Connections.conns == []
                           d.``done``())
                       |> ignore)
        |> ignore)
    |> ignore
