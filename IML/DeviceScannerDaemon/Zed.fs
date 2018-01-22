// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Zed

open Fable.Core
open IML.JsonDecoders
open IML.StringUtils
open Fable.Import.Node.PowerPack.Stream
open Thot.Json.Decode

[<RequireQualifiedAccess>]
module Result =

  let isOk = function
    | Ok _ -> true
    | Error _ -> false

  let isError =
     isOk >> not

let private unwrap x =
    match x with
      | Ok y -> y
      | Error y -> failwith y

let private historyInternalNameDecoder (x:LineDelimitedJson.Json) =
  decodeJson (field "ZEVENT_HISTORY_INTERNAL_NAME" string) x

[<RequireQualifiedAccess>]
module Zpool =
  [<Erase>]
  type Guid = Guid of string

  let private subclassDecoder =
    decodeJson (field "ZEVENT_SUBCLASS" string)

  let private stateStrDecoder =
    field "ZEVENT_POOL_STATE_STR" string

  let guidDecoder =
    field "ZEVENT_POOL_GUID" string
      |> map Guid

  let isExportState x =
    decodeJson stateStrDecoder x = Ok("EXPORTED")

  let private isExportClass x = 
      subclassDecoder x = Ok("pool_export") 

  let isDestroyState x =
    decodeJson stateStrDecoder x = Ok("DESTROYED")

  let private isDestroyClass x =
    subclassDecoder x = Ok("pool_destroy")

  /// Representation of a zpool from ZED events.
  type Data =
    {
      /// Name of the pool
      name: string;
      /// Unique id of the imported pool.
      /// Exporting and importing the pool
      /// will change this guid.
      guid: Guid;
      /// The state of the pool.
      state: string;
    }

  let dataDecoder x =
    let decoder =
      map3(fun name guid state ->
        {
          name = name;
          guid = guid;
          state = state;
        })
        (field "ZEVENT_POOL" string)
        guidDecoder
        stateStrDecoder
      |> decodeJson

    decoder x |> unwrap

  let (|Create|_|) x =
    match subclassDecoder x with
      | Ok("pool_create") ->
        Some (dataDecoder x)
      | _ -> None

  let (|Import|_|) x =
    match subclassDecoder x with
      | Ok("pool_import") ->
        Some (dataDecoder x)
      | _ -> None

  let (|Export|_|) x =
      if isExportClass x && isExportState x then
        Some (dataDecoder x)
      else
        None

  let (|Destroy|_|) x =
    if isDestroyClass x && isDestroyState x then
      Some (dataDecoder x)
    else
      None

[<RequireQualifiedAccess>]
module Zfs =
  [<Erase>]
  type Id = Id of string

  let dsIdDecoder =
    field "ZEVENT_HISTORY_DSID" string

  type Data =
    {
      /// The pool the zfs item belongs to.
      poolGuid: Zpool.Guid;
      /// The name of the zfs item
      name: string;
      /// Id of the zfs item.
      id: Id;
    }

  let dataDecoder x =
    let decoder =
        map3 (fun poolGuid name id ->
            {
              poolGuid = poolGuid;
              name = name;
              id = id;
            })
            Zpool.guidDecoder
            (field "ZEVENT_HISTORY_DSNAME" string)
            (dsIdDecoder |> map Id)
        |> decodeJson

    decoder x |> unwrap

  let (|Create|_|) x =
    if historyInternalNameDecoder x = Ok("create") &&
        (dsIdDecoder >> Result.isOk) x then
          Some (dataDecoder x)
    else
      None

  let (|Destroy|_|) x =
    if historyInternalNameDecoder x = Ok("destroy") &&
        (dsIdDecoder >> Result.isOk) x then
          Some (dataDecoder x)
    else
      None

[<RequireQualifiedAccess>]
module Properties =
  /// Representation of a ZFS property.
  /// This is a nvpair in ZFS.
  type ZfsProperty =
    {
      /// The guid of the pool this property is associated with.
      poolGuid: Zpool.Guid;
      /// The zfs item id this property is associated with.
      zfsId: Zfs.Id;
      /// The property name
      name: string;
      /// The property value
      value: string;
    }

  /// Representation of a zpool property.
  /// This is a nvpair in ZFS.
  type ZpoolProperty =
    {
      /// The guid of the pool this property is associated with.
      poolGuid: Zpool.Guid;
      /// The property name
      name: string;
      /// The property value
      value: string;
    }

  [<Erase>]
  type Property =
    | Zpool of ZpoolProperty
    | Zfs of ZfsProperty

  let byPoolGuid x y =
        match y with
          | Zfs p -> p.poolGuid <> x
          | Zpool p -> p.poolGuid <> x

  let private nvpairDecoder =
    let toTuple xs =
      (Array.head xs, Array.last xs)

    field "ZEVENT_HISTORY_INTERNAL_STR" string
      |> map (split [| '=' |] >> toTuple)

  let zpoolPropertyDecoder x =
    let decoder =
      map2 (fun guid (name, value) ->
        {
          poolGuid = guid;
          name = name;
          value = value;
        })
        Zpool.guidDecoder
        nvpairDecoder
      |> decodeJson

    decoder x |> unwrap

  let zfsPropertyDecoder x =
    let decoder =
      map3 (fun poolUid datasetUid (name, value) ->
        {
          poolGuid = poolUid;
          zfsId = datasetUid;
          name = name;
          value = value;
        })
        Zpool.guidDecoder
        (Zfs.dsIdDecoder |> map Zfs.Id)
        nvpairDecoder
      |> decodeJson

    decoder x |> unwrap

  let private isSet x =
    historyInternalNameDecoder x = Ok("set")

  let (|ZpoolProp|_|) x =
    if decodeJson Zfs.dsIdDecoder x |> Result.isError && isSet x then
      Some (Property.Zpool (zpoolPropertyDecoder x))
    else
      None

  let (|ZfsProp|_|) x =
    if decodeJson Zfs.dsIdDecoder x |> Result.isOk && isSet x then
      Some (Property.Zfs (zfsPropertyDecoder x))
    else
      None

let (|ZedGeneric|_|) x = 
  if decodeJson (field "ZEVENT_EID" string) x |> Result.isOk then
    printfn "Got generic ZED event %A" x
    Some ()
  else 
    None 