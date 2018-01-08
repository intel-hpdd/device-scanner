// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open Fable.Import.Node.PowerPack.Stream
open Udev
open Zed


type Data = {
  mutable blockDevices: Map<DevPath, UEvent>;
  mutable zpools: Map<Zpool.Guid, Zpool.Data>;
  mutable zfs: Map<Zfs.Id, Zfs.Data>;
  mutable props: Properties.Property list
}

let data = {
  blockDevices = Map.empty;
  zpools = Map.empty;
  zfs = Map.empty;
  props = [];
}

let (|Info|_|) (x:LineDelimitedJson.Json) =
  match actionDecoder x with
    | Ok(y) when y = "info" -> Some()
    | _ -> None

let dataHandler (x:LineDelimitedJson.Json) =
  match x with
    | Info -> ()
    | UdevAdd x | UdevChange x ->
      data.blockDevices <- Map.add x.DEVPATH x data.blockDevices

    | UdevRemove x ->
      data.blockDevices <- Map.remove x.DEVPATH data.blockDevices

    | Zpool.Create x ->
      data.zpools <- Map.add x.guid x data.zpools

    | Zpool.Import x | Zpool.Export x ->
      data.zpools <- Map.add x.guid x data.zpools

    | Zpool.Destroy x ->
      data.zpools <- Map.remove x.guid data.zpools
      data.props <- List.filter (Properties.byPoolGuid x.guid) data.props
      data.zfs <- Map.filter (fun _ z -> z.poolGuid <> x.guid) data.zfs

    | Zfs.Create x ->
      data.zfs <- Map.add x.id x data.zfs

    | Zfs.Destroy x ->
      data.zfs <- Map.remove x.id data.zfs

      let filterZfsProps (x:Zfs.Data) y =
        match y with
          | Properties.Zfs p ->
            p.poolGuid <> x.poolGuid && p.zfsId <> x.id
          | _  -> true

      data.props <- List.filter (filterZfsProps x) data.props
    | Properties.ZpoolProp (x:Properties.Property) ->
      data.props <- (data.props @ [x])
    | Properties.ZfsProp x ->
      data.props <- (data.props @ [x])
    | ZedGeneric -> ()
    | _ ->
      failwith "Handler got a bad match"

  Ok data
