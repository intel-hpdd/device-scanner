// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.ProxyHandlers

open Fable.Import.Node
open Fable.Core.JsInterop
open System

module Option =
  let expect message = function
    | Some x -> x
    | None -> failwithf message

let dataHandler (x:string) =
  x |> buffer.Buffer.from |> Ok

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
      (fun x -> (fs.readFileSync (dirName + x)).toString())
        >> ofJson<Collections.Map<string,string>>
        >> parseUrl
    )
    |> Seq.tryHead
    |> Option.expect "did not find 'server' file"
