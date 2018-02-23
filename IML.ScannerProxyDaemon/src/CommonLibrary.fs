// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.ScannerProxyDaemon.CommonLibrary

module Option =
  let expect message = function
    | Some x -> x
    | None -> failwithf message

type Message =
  | Data of string
  | Heartbeat
