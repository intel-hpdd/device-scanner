// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open Fable.Core.JsInterop
open Fable.PowerPack
open System.Collections.Generic

open EventTypes
open IML.JsonDecoders
open IML.DeviceScannerDaemon.EventTypes
open IML.DeviceScannerDaemon.ZFSEventTypes
open Fable.Import.Node.Base.NodeJS
open Fable.Import.JS
open System.Xml
open System.Xml.Xsl
open System.Runtime.Serialization
open IML.DeviceScannerDaemon.ZFSEventTypes

let private deviceMap = Dictionary<DevPath, AddEvent>()
let private zpoolMap = Dictionary<ZeventGuid, ZfsPool>()

let (|Info|_|) (x:Map<string,Json.Json>) =
  match x with
    | x when hasAction "info" x -> Some()
    | _ -> None
type DataMaps = {
  BLOCK_DEVICES: Dictionary<DevPath, AddEvent>;
  ZFSPOOLS: Dictionary<ZeventGuid, ZfsPool>;
}

let poolFromEvent (x:ZedPoolEvent) =
  {
    NAME = x.ZEVENT_POOL;
    UID = x.ZEVENT_POOL_GUID;
    STATE_STR = x.ZEVENT_POOL_STATE_STR;
    PATH = x.ZEVENT_POOL;
    DATASETS = Dictionary<ZeventGuid, ZfsDataset>();
  }

let private handlePoolEvent (x:ZedPoolEvent) =
  match x.ZEVENT_CLASS with
    | ZeventClass("sysevent.fs.zfs.pool_create") -> zpoolMap.Add (x.ZEVENT_POOL_GUID, poolFromEvent x)
    //poolCreateClass.ToString() -> zpoolMap.Add (x.ZEVENT_POOL_GUID, poolFromEvent x)
    | ZeventClass("sysevent.fs.zfs.pool_destroy") -> zpoolMap.Remove x.ZEVENT_POOL_GUID |> ignore
    //| poolDestroyClass.ToString() -> zpoolMap.Remove x.ZEVENT_POOL_GUID |> ignore
    | _  -> raise (System.Exception "ZfsPool handler got a bad match")

//let private matchZpool x =
//  match zpoolMap.TryGetValue x with
//    | true, value -> Some(value)
//    | _ -> None
//
//let private modifyZpoolState x y =
//  zpoolMap.Remove x.UID |> ignore
//  zpoolMap.Add (x.UID, {
//                         NAME = x.NAME;
//                         UID = x.UID;
//                         STATE_STR = y;
//                         PATH = x.PATH;
//                         DATASETS = x.DATASETS;
//                       })

let dataHandler' (``end``:string option -> unit) = function
  | InfoEventMatch(_) ->
    ``end`` (Some (toJson {
                            BLOCK_DEVICES = deviceMap;
                            ZFSPOOLS = zpoolMap;
                          }))
  | AddOrChangeEventMatch(x) ->
    deviceMap.Add (x.DEVPATH, x)
    ``end`` None
  | RemoveEventMatch(x) ->
    deviceMap.Remove x.DEVPATH |> ignore
    ``end`` None
  | ZedHistoryEventMatch(x) ->
    ``end`` None
  | ZedPoolEventMatch(x) ->
    console.log(x)
    handlePoolEvent x |> ignore
    ``end`` None
  | ZedGenericEventMatch(x) ->
    ``end`` None
//  | ZedPoolCreateEventMatch(x) ->
//    printfn "PoolCreateEventMatch"
//    zpoolMap.Add (x.UID, x)
//    ``end`` None
//  | ZedPoolDestroyEventMatch(x) ->
//    printfn "PoolDestroyEventMatch"
//    zpoolMap.Remove x.UID |> ignore
//    ``end`` None
//  | ZedPoolImportEventMatch(x) ->
//    printfn "PoolImportEventMatch"
//    let zpool = matchZpool x.UID
//
//    match zpool with
//       Some pool -> modifyZpoolState pool x.STATE_STR |> ignore
//      | None -> zpoolMap.Add (x.UID, x)
//    ``end`` None
//  | ZedPoolExportEventMatch(x) ->
//    printfn "PoolExportEventMatch"
//    let zpool = matchZpool x.UID
//
//    match zpool with
//      | Some pool -> modifyZpoolState pool x.STATE_STR |> ignore
//      | None -> zpoolMap.Add (x.UID, x)
//    ``end`` None
//  | ZedDatasetCreateEventMatch(x) ->
//    printfn "DatasetCreateEventMatch"
//    let zpool = matchZpool x.POOL_UID
//
//    match zpool with
//      | Some pool -> pool.DATASETS.Add (x.DATASET_UID, x)
//      | None -> printfn "Pool with uid=%A missing, cant add dataset" x.POOL_UID
//    ``end`` None
//  | ZedDatasetDestroyEventMatch(x) ->
//    printfn "DatasetDestroyEventMatch"
//    let zpool = matchZpool x.POOL_UID
//
//    match zpool with
//      | Some pool -> pool.DATASETS.Remove x.DATASET_UID |> ignore
//      | None -> printfn "Pool with uid=%A missing, cant remove dataset" x.POOL_UID
//    ``end`` None
  | _ ->
    ``end`` None
    raise (System.Exception "Handler got a bad match")

let dataHandler (``end``:string option -> unit) x =
  x
   |> unwrapObject
   |> function
      | Info ->
        ``end`` (Some (toJson deviceMap))
      | UdevAdd(x) | UdevChange(x) ->
        deviceMap.Add (x.DEVPATH, x)
        ``end`` None
      | UdevRemove(x) ->
        deviceMap.Remove x |> ignore
        ``end`` None
      | _ ->
        ``end`` None
        raise (System.Exception "Handler got a bad match")
