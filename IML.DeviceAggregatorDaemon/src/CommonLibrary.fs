// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.CommonLibrary

open Fable.Core

type Message =
  | Data of string
  | Heartbeat

[<Erase>]
type Hostname = Hostname of string
