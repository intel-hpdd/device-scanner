// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Listeners.CommonLibrary

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.Node
open IML.Types.CommandTypes

let sendData (x:Command) =
  let opts = createEmpty<Net.ConnectOptions>
  opts.path <- Some "/var/run/device-scanner.sock"

  let client = net.connect opts

  client.once("connect", fun () ->
  x
    |> toJson
    |> buffer.Buffer.from
    |> client.``end``
  )
    |> ignore


let env = Globals.``process``.env

module Udev =
  [<StringEnum>]
  type UdevActions =
    | Add
    | Change
    | Remove

  let getAction():UdevActions = !!env?ACTION

[<RequireQualifiedAccess>]
module Zpool =
  let getGuid () =
    !!env?ZEVENT_POOL_GUID
      |> Zpool.Guid

  let getState() =
    !!env?ZEVENT_POOL_STATE_STR
      |> Zpool.State

  let getName() =
    !!env?ZEVENT_POOL
      |> Zpool.Name

[<RequireQualifiedAccess>]
module Zfs =
  let getName() =
    !!env?ZEVENT_HISTORY_DSNAME
      |> Zfs.Name

  let getNameOption():Zfs.Name option =
    !!env?ZEVENT_HISTORY_DSNAME
      |> Option.map Zfs.Name

[<RequireQualifiedAccess>]
module Vdev =
  let getGuid() =
    !!env?ZEVENT_VDEV_GUID
      |> Vdev.Guid

  let getState() =
    !!env?ZEVENT_VDEV_STATE_STR
      |> Vdev.State

[<RequireQualifiedAccess>]
module Zed =
  [<StringEnum>]
  type HistoryEvents =
    | Create
    | Destroy
    | Set

  let getHistoryName():HistoryEvents = !!env?ZEVENT_HISTORY_INTERNAL_NAME
  let getHistoryStr():string = !!env?ZEVENT_HISTORY_INTERNAL_STR
