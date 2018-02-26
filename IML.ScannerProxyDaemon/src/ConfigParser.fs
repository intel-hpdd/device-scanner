// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.ConfigParser

open Fable.Core.JsInterop
open Fable.Import.Node
open System

open CommonLibrary

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

let libPath x = path.join(path.sep, "var", "lib", "chroma", x)

let readConfigFile =
  libPath >> fs.readFileSync
