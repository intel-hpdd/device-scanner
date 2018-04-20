// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.ConfigParser

open Fable.Core.JsInterop
open Fable.Import.Node
open Thoth.Json

open IML.CommonLibrary

let filterFileName name =
  Seq.filter (fun x -> (buffer.Buffer.from(x, "base64").toString()) = name)

let serverDecoder =
  (Decode.decodeString
    (Decode.map
      (fun x ->
        let r = url.parse x

        r.hostname
          |> Option.expect "did not find hostname"
      )
      (Decode.field "url" Decode.string)
    ))
    >> Result.unwrap

let getManagerUrl dirName =
  fs.readdirSync !^ dirName
    |> Seq.toList
    |> filterFileName "server"
    |> Seq.map (
      (fun x -> (fs.readFileSync (path.join(dirName, x))).toString())
        >> serverDecoder
    )
    |> Seq.tryHead
    |> Option.expect "did not find 'server' file"

let libPath x = path.join(path.sep, "var", "lib", "chroma", x)

let readConfigFile =
  libPath >> fs.readFileSync
