// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open IML.Types.CommandTypes
open Udev
open Zed

let private scan init update =
  let mutable state = init()

  fun (x) ->
    state <- update state x
    state

type Data = {
  blockDevices: BlockDevices;
  zed: Zed.ZedData;
}

let init () =
  {
    blockDevices = Map.empty;
    zed = 
      {
        zpools = Map.empty;
        zfs = Set.empty;
        props = Set.empty;
      };
  }

let update (state:Data) (command:Command):Data =
    match command with
      | ZedCommand x ->
        { state with 
            zed = Zed.update state.zed x;
        }
      | UdevCommand x ->
        { state with 
            blockDevices = Udev.update state.blockDevices x;
        }
      | Info ->
        state

let handler = scan init update