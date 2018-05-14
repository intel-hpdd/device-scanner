// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.Transmit

open Fable.Core.JsInterop
open Fable.Import.Node
open PowerPack.Stream
open IML.Types.MessageTypes

let private opts = createEmpty<Https.RequestOptions>

opts.hostname <- Some Config.managerUrl
opts.port <- Some 443
opts.path <- Some "/iml-device-aggregator"
opts.method <- Some Http.Methods.Post
opts.rejectUnauthorized <- Some false
opts.cert <- Some Config.cert
opts.key <- Some Config.key
opts.headers <- Some(createObj [ "Content-Type" ==> "application/json" ])

let transmit (payload : Message) =
    https.request opts
    |> Readable.onError
           (fun (e : exn) ->
           eprintfn "Unable to generate HTTPS request %s, %s" e.Message
               e.StackTrace)
    |> Writable.``end`` (payload
                         |> Message.encoder
                         |> Some)
