// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTestFramework.Config

open Fable.Core.JsInterop
open Fable.Import.Node
open IML.CommonLibrary

let managerUrl : string =
    !!Globals.``process``.env?IML_MANAGER_URL
    |> Option.bind (fun x -> (url.parse x).hostname)
    |> Option.expect "Did not find IML_MANAGER_URL with hostname"

let cert = fs.readFileSync !!Globals.``process``.env?IML_CERT_PATH :> obj
let key = fs.readFileSync !!Globals.``process``.env?IML_PRIVATE_KEY :> obj
