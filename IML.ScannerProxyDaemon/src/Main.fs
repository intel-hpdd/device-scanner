// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.Proxy

open Fable.Core.JsInterop
open Fable.Import.Node
open PowerPack.Stream

open ProxyHandlers

let private libPath x = path.join("var", "lib", "chroma", x)

let private readConfigFile (x) =
  (fs.readFileSync (libPath x)) :> obj

let private getOpts () =
  let opts = createEmpty<Https.RequestOptions>
  opts.hostname <- Some (getManagerUrl (libPath "settings"))
  opts.port <- Some 443
  opts.path <- Some "/iml-device-aggregator"
  opts.method <- Some Http.Methods.Post
  opts.rejectUnauthorized <- Some false
  opts.cert <- Some (readConfigFile "self.crt")
  opts.key <- Some (readConfigFile "private.pem")
  let headers =
    createObj [
      "Content-Type" ==> "application/json"
    ]
  opts.headers <- Some headers
  opts

let sendPostRequest data =
  printfn "writing update in POST to device-aggregator endpoint"
  https.request (getOpts())
    |> Readable.onError (fun (e:exn) ->
      eprintfn "Unable to generate HTTPS request %s, %s" e.Message e.StackTrace
    )
    |> Writable.``end`` (Some data)

let clientSock = net.connect("/var/run/device-scanner.sock")
printfn "Connecting to device scanner..."

clientSock
  |> LineDelimited.create()
  |> Readable.onError (fun (e:exn) ->
    eprintfn "Unable to parse Json from device scanner %s, %s" e.Message e.StackTrace
  )
  |> map dataHandler
  |> iter sendPostRequest
  |> ignore

clientSock
  |> Writable.write (buffer.Buffer.from "\"Info\"\n")
  |> ignore

