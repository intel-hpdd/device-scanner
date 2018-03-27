// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.


module IML.CommonLibraryTest


open IML.CommonLibrary
open Fable.Import.Jest
open Matchers

test "hex parse to string" <| fun () ->
  let xs =
    [
      ("0x6b289bd5ee51b853", "7721592904255387731");
      ("0xdea49846eb1a8aaf", "16043115202960067247");
      ("0x3749f664e938d727", "3983986258655893287");
      ("0x2c8f8932d2d7215b", "3210935910717137243");
      ("0x4285ace978265214", "4793427497148895764");
      ("0xe80bed31206e2ca2", "16720718836796370082");
      ("0x7d83835a55ba3f23", "9044216900698652451");
      ("0xffeacb0159335355", "18440774830873858901");
      ("0xa683cd2a283e48d5", "11998659413192624341");
      ("0xa929ace0b7509a8c", "12189463947603122828");
    ]

  expect.assertions (xs.Length)

  Set.ofList xs
    |> Set.iter(fun (l, r) -> (Hex.toBignumString l) === r)
