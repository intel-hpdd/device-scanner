// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.QueryTest

open Fable.Import.Jest
open Matchers
open Query

let testServerHost = "localhost"
let testServerPort = 8181


test "host has pool disks"
<| fun () ->
    toEqual
        (matchPaths [ "/foo/bar"; "/foo/baz"; "/bar/baz" ]
             [ "/foo/bar"; "/bar/baz" ], true) |> ignore
test "host doesn't have pool disks"
<| fun () ->
    toEqual
        (matchPaths [ "/foo/bar"; "/foo/baz"; "/bar/baz" ]
             [ "/foo/bar"; "/bar/boz" ], true) |> ignore
test "Discover no pools on host"
<| fun () ->
    discoverZpools Map.empty "ffo.com" Map.empty Map.empty List.empty
    |> toMatchSnapshot
