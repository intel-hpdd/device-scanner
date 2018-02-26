// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open IML.Types.CommandTypes
open Fable.Core
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
  Ok {
    blockDevices = Map.empty;
    zed =
      {
        zpools = Map.empty;
        zfs = Set.empty;
        props = Set.empty;
      };
  }

let update (state:Result<Data, exn>) (command:Command):Result<Data, exn> =
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
          | Command.Info | ACTION _ ->
            Ok state
      | x -> x

let private handler = scan init update

[<Erase>]
type BackCompat =
  | V1 of BlockDevices
  | Vx of Data

let backCompatHandler x =
  x
  |> handler
  |> Result.map (fun d ->
    match x with
      | ACTION _ -> V1 d.blockDevices
      | _ -> Vx d
  )
