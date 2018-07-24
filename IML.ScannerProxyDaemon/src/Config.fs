// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.Config

open Fable.Core.JsInterop
open Fable.Import.Node
open IML.CommonLibrary

let private envUrl : string = !!Globals.``process``.env?IML_MANAGER_URL
let private parts = url.parse envUrl

let managerUrl : string =
    parts.hostname
    |> Option.expect "Did not find IML_MANAGER_URL with hostname"

let port : string =
  parts.port
  |> Option.expect "Did not find IML_MANAGER_URL with port"

let cert = fs.readFileSync !!Globals.``process``.env?IML_CERT_PATH :> obj
let key = fs.readFileSync !!Globals.``process``.env?IML_PRIVATE_KEY :> obj
