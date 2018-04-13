// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.Proxy

open Fable.Import.Node
open PowerPack.Stream
open Fable.Import

open IML.Types.MessageTypes
open Transmit

JS.setInterval (fun _ -> transmit Heartbeat) 10000
  |> ignore

let clientSock = net.connect("/var/run/device-scanner.sock")
printfn "Proxy connecting to device scanner..."

clientSock
  |> LineDelimited.create()
  |> Readable.onError (fun (e:exn) ->
    eprintfn "Unable to parse Json from device scanner %s, %s" e.Message e.StackTrace
  )
  |> iter (Data >> transmit)
  |> ignore

clientSock
  |> Writable.write (buffer.Buffer.from "\"Stream\"\n")
  |> ignore
