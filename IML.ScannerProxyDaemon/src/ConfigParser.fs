// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.ConfigParser

open Fable.Core.JsInterop
open Fable.Import.Node

open IML.CommonLibrary

let filterFileName name =
  Seq.filter (fun x -> (buffer.Buffer.from(x, "base64").toString()) = name)

let managerUrl: string =
  !!Globals.``process``.env?MANAGER_URL
    |> Option.bind (fun x ->
      (url.parse x).hostname
    )
    |> Option.expect "Did not find MANAGER_URL with hostname"

let libPath x = path.join(path.sep, "var", "lib", "chroma", x)

let readConfigFile =
  libPath >> fs.readFileSync
