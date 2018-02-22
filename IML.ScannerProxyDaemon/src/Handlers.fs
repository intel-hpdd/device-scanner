// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.Handlers

open Fable.Core.JsInterop
open Fable.Import.Node
open PowerPack.Stream
open System

module Option =
  let expect message = function
    | Some x -> x
    | None -> failwithf message

let filterFileName name =
  Seq.filter (fun x -> (buffer.Buffer.from(x, "base64").toString()) = name)

let parseUrl (xs:Collections.Map<string,string>) =
  let url =
    xs.TryFind "url"
      |> Option.expect "url not found"

  url
    .Replace("https://", "")
    .Split([| ':' |])
      |> Array.tryHead
      |> Option.expect "url did not contain a colon"

let getManagerUrl dirName =
  fs.readdirSync !^ dirName
    |> Seq.toList
    |> filterFileName "server"
    |> Seq.map (
      (fun x -> (fs.readFileSync (path.join(dirName, x))).toString())
        >> ofJson<Collections.Map<string,string>>
        >> parseUrl
    )
    |> Seq.tryHead
    |> Option.expect "did not find 'server' file"

let private libPath x = path.join(path.sep, "var", "lib", "chroma", x)

let private readConfigFile =
  libPath >> fs.readFileSync

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

type Message =
  | Data of string
  | Heartbeat

let transmit payload =
  https.request (getOpts())
    |> Readable.onError (fun (e:exn) ->
      eprintfn "Unable to generate HTTPS request %s, %s" e.Message e.StackTrace
    )
    |> Writable.``end``(Some payload)

let sendMessage = function
  | Heartbeat ->
    printfn "Proxy heartbeat %A" (toJson Heartbeat)
    transmit (toJson Heartbeat)
  | Data x ->
    printfn "Proxy update %A" (toJson (Data x))
    transmit (toJson (Data x))

let createTimer timerInterval eventHandler =
    let timer = new System.Timers.Timer(float timerInterval)
    timer.AutoReset <- true

    timer.Elapsed.Add eventHandler

    async {
        timer.Start()
    }
