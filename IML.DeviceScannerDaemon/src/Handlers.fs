// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open IML.Types.CommandTypes
open IML.Types.ScannerStateTypes

open Zed

let private scan init update =
  let mutable state = init()

  fun (x) ->
    state <- update state x
    state

let init () =
  Ok {
    blockDevices = Map.empty;
    zed = Map.empty;
    localMounts = Set.empty;
  }

let update (state:Result<State, exn>) (command:Command):Result<State, exn> =
    match state with
      | Ok state ->
        match command with
          | ZedCommand x ->
            Zed.update state.zed x
              |> Result.map (fun zed ->
                { state with
                    zed = zed;
                }
              )
          | UdevCommand x ->
            Udev.update state.blockDevices x
              |> Result.map (fun blockDevices ->
                { state with
                    blockDevices = blockDevices;
                }
              )
          | MountCommand x ->
            Mount.update state.localMounts x
              |> Result.map (fun localMounts ->
                { state with
                    localMounts = localMounts;
                }
              )
          | Command.Stream ->
            Ok state
      | x -> x

let handler = scan init update
