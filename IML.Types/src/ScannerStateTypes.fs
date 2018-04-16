// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.ScannerStateTypes

open Thot.Json

open IML.Types.UeventTypes
open IML.Types.MountTypes
open IML.Types.ZedTypes


type State = {
  blockDevices: BlockDevices;
  zed: Zed;
  localMounts: LocalMounts;
}

module State =
  let encode
    {
      blockDevices = blockDevices;
      zed = zed;
      localMounts = localMounts;
    } =
      Encode.object [
        ("zed", Zed.encoder zed)
        ("blockDevices", BlockDevices.encoder blockDevices)
        ("localMounts", LocalMounts.encoder localMounts)
      ]

  let encoder =
    encode
      >> Encode.encode 0

  let decoder x =
    Decode.map3 (fun blockDevices zed localMounts ->
      {
        blockDevices = blockDevices;
        zed = zed;
        localMounts = localMounts;
      })
      (Decode.field "blockDevices" BlockDevices.decoder)
      (Decode.field "zed" Zed.decoder)
      (Decode.field "localMounts" LocalMounts.decoder)
      x
