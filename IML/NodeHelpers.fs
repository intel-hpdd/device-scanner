// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module rec NodeHelpers

open Fable.Import.Node
open Fable.Core


  [<AutoOpen>]
  module NetHelpers =
    [<Pojo>]
    type NetPath = {
      path: string
    }

    let ``end`` (c:Net.Socket) = function
      | Some(x) -> c.``end``(x)
      | None -> c.``end``()

    let onceConnect (fn:unit -> unit) (c:Net.Socket)  = c.once("connect", fn) :?> Net.Socket

    let onConnect (fn:unit -> unit) (c:Net.Socket)  = c.on("connect", fn) :?> Net.Socket

    let connect (x:NetPath) = Net.connect x
