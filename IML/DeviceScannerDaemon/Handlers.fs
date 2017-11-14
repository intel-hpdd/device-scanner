// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open Fable.Core.JsInterop
open Fable.PowerPack

open EventTypes
open IML.JsonDecoders
open ZFSEventTypes


let mutable deviceMap:Map<DevPath, AddEvent> = Map.empty
let mutable zpoolMap:Map<ZeventGuid, ZfsPool> = Map.empty

let (|Info|_|) (x:Map<string,Json.Json>) =
  match x with
    | x when hasAction "info" x -> Some()
    | _ -> None
type DataMaps = {
  BLOCK_DEVICES: Map<DevPath, AddEvent>;
  ZFSPOOLS: Map<ZeventGuid, ZfsPool>;
}

let dataHandler (``end``:string option -> unit) x =
  x
   |> unwrapObject
   |> function
      | Info ->
        { BLOCK_DEVICES = deviceMap; ZFSPOOLS = zpoolMap }
          |> toJson
          |> Some
          |> ``end``
      | UdevAdd(x) | UdevChange(x) ->
        deviceMap <- Map.add x.DEVPATH x deviceMap
        ``end`` None
      | UdevRemove(x) ->
        deviceMap <- Map.remove x deviceMap
        ``end`` None
      | ZedPool "create" x ->
        zpoolMap <- Map.add x.UID x zpoolMap
        ``end`` None
      | ZedPool "import" x | ZedExport x ->
        let updatedPool =
          match Map.tryFind x.UID zpoolMap with
            | Some pool ->
              { x with DATASETS = pool.DATASETS }
            | None -> x

        zpoolMap <- Map.add x.UID updatedPool zpoolMap
        ``end`` None
      | ZedDestroy x ->
        zpoolMap <- zpoolMap.Remove x.UID
      | ZedHistory(x) ->
        x.ZEVENT_HISTORY_DSID
          |> Option.map (fun _ ->
             handleDatasetEvent (datasetFromEvent x) (x.ZEVENT_HISTORY_INTERNAL_NAME.ToString()) zpoolMap)
          |> ignore
        ``end`` None
      | ZedGeneric(_) ->
        ``end`` None
      | _ ->
        ``end`` None
        raise (System.Exception "Handler got a bad match")
