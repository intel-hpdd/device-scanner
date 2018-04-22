// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Udev

open IML
open Types.CommandTypes
open Types.UeventTypes

let update (blockDevices:BlockDevices) (x:UdevCommand):Result<BlockDevices, exn> =
  match x with
    | Add o | Change o ->
      UEvent.udevDecoder o
        |> Result.map (fun d ->
          blockDevices
            |> Map.add d.devpath d
            |> BlockDevices.linkParents
        )
    | Remove o ->
      UEvent.udevDecoder o
        |> Result.map (fun d ->
          Map.remove d.devpath blockDevices
        )
