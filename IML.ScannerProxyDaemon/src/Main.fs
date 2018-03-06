// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.Proxy

open Fable.Import.Node
open PowerPack.Stream
open Fable.Import

open CommonLibrary
open Transmit

JS.setInterval (fun _ -> transmitMessage Heartbeat) 10000
  |> ignore

let clientSock = net.connect("/var/run/device-scanner.sock")
printfn "Proxy connecting to device scanner..."

clientSock
  |> LineDelimited.create()
  |> Readable.onError (fun (e:exn) ->
    eprintfn "Unable to parse Json from device scanner %s, %s" e.Message e.StackTrace
  )
  |> iter (Data >> transmitMessage)
  |> ignore

clientSock
  |> Writable.write (buffer.Buffer.from "\"Info\"\n")
  |> ignore
