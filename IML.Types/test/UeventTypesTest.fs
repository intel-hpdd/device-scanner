// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.UeventTypesTest

open UeventTypes

open Fable.Import.Jest
open IML.CommonLibrary
open Matchers
open Fixtures

[
  ("add", fixtures.add);
  ("add DM", fixtures.addDm);
  ("remove", fixtures.remove);
  ("MdRaid", fixtures.addMdRaid);
]
  |> List.map (fun ((name, x)) ->
    Test(name, fun () ->
      expect.assertions 2

      let decoded =
        x
        |> UEvent.udevDecoder
        |> Result.unwrap

      toMatchSnapshot decoded

      Map.ofList [
          (decoded.devpath, decoded);
        ]
        |> BlockDevices.encoder
        |> Thot.Json.Encode.encode 2
        |> toMatchSnapshot
    )
  )
  |> testList "decode / encode UEvents "
