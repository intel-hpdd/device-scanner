// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.MountEmitter.Main

open IML.Listeners.CommonLibrary
open Transform
open Fable.Import.Node
open Fable.Import.Node.PowerPack


Globals.``process``.stdin
  |> transform
  |> Stream.iter sendData
  |> ignore
