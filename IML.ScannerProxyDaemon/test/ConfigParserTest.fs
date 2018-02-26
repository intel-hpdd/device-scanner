// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.ConfigParserTest

open Fable.Import.Jest
open Matchers

open ConfigParser

testList "Data Handler" [
  Test("Should filter out all but the base64 encoded string of 'server'", fun () ->
    [| "foo"; "bar"; "c2VydmVy" |]
      |> (filterFileName "server")
      |> Seq.toArray
      |> toMatchSnapshot
  )

  Test("Should parse url from map", fun () ->
    Map.empty
      |> Map.add "url" "https://foo.com:443/agent/"
      |> parseUrl
      |> toMatchSnapshot
  )
]
