// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.Transmit

open Fable.Core.JsInterop
open Fable.Import.Node
open PowerPack.Stream

open CommonLibrary
open ConfigParser

let private getOpts () =
  let opts = createEmpty<Https.RequestOptions>
  opts.hostname <- Some (getManagerUrl (libPath "settings"))
  opts.port <- Some 443
  opts.path <- Some "/iml-device-aggregator"
  opts.method <- Some Http.Methods.Post
  opts.rejectUnauthorized <- Some false
  opts.cert <- Some (readConfigFile "self.crt" :> obj)
  opts.key <- Some (readConfigFile "private.pem" :> obj)
  let headers =
    createObj [
      "Content-Type" ==> "application/json"
    ]
  opts.headers <- Some headers
  opts

let private transmit payload =
  https.request (getOpts())
    |> Readable.onError (fun (e:exn) ->
      eprintfn "Unable to generate HTTPS request %s, %s" e.Message e.StackTrace
    )
    |> Writable.``end``(Some payload)

let sendMessage transmitFn x =
  match x with
  | Heartbeat ->
    transmitFn (toJson Heartbeat)
  | Data y ->
    transmitFn (toJson (Data y))

let transmitMessage = sendMessage transmit
