// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open Fable.Import.Node
open Fable.Core.JsInterop
open Fable.PowerPack

open EventTypes
open IML.JsonDecoders
open ZFSEventTypes

let mutable deviceMap:Map<DevPath, AddEvent> = Map.empty
let mutable zpoolMap:Map<ZfsPoolUid, ZfsPool> = Map.empty

type DataMaps = {
  BLOCK_DEVICES: Map<DevPath, AddEvent>;
  ZFSPOOLS: Map<ZfsPoolUid, ZfsPool>;
}

type DatasetAction = CreateDataset | DestroyDataset

let private (|Info|_|) (x:Map<string,Json.Json>) =
  match x with
    | x when hasAction "info" x -> Some()
    | _ -> None

let private (|Stream|_|) (x:Map<string,Json.Json>) =
  match x with
    | x when hasAction "stream" x -> Some()
    | _ -> None

let private updateDatasets (action:DatasetAction) (x:ZfsDataset) =
  let matchAction pool =
    match action with
      | CreateDataset -> pool.DATASETS.Add (x.DATASET_UID, x)
      | DestroyDataset -> pool.DATASETS.Remove x.DATASET_UID

  match Map.tryFind x.POOL_UID zpoolMap with
    | Some pool ->
      { pool with DATASETS = matchAction pool }
    | None -> failwith (sprintf "Pool to update dataset on is missing! %A" x.POOL_UID)

let mutable private closeOnWrite = true

let private getState =
  { BLOCK_DEVICES = deviceMap; ZFSPOOLS = zpoolMap }
    |> toJson
    |> Some

let dataHandler (sock:Net.Socket) x =
  x
    |> unwrapObject
    |> function
      | Info -> (closeOnWrite <- true)
      | Stream -> (closeOnWrite <- false)
      | UdevAdd x | UdevChange x -> (deviceMap <- Map.add x.DEVPATH x deviceMap)
      | UdevRemove x -> (deviceMap <- Map.remove x deviceMap)
      | ZedPool "create" x -> (zpoolMap <- Map.add x.UID x zpoolMap)
      | ZedPool "import" x | ZedExport x ->
        let updatedPool =
          match Map.tryFind x.UID zpoolMap with
            | Some pool ->
              { x with DATASETS = pool.DATASETS; PROPERTIES = pool.PROPERTIES }
            | None -> x

        zpoolMap <- Map.add x.UID updatedPool zpoolMap
      | ZedDestroy x -> (zpoolMap <- zpoolMap.Remove x.UID)
      | ZedDataset "create" x ->
        let updatedPool = updateDatasets CreateDataset x

        zpoolMap <- Map.add x.POOL_UID updatedPool zpoolMap
      | ZedDataset "destroy" x ->
        let updatedPool = updateDatasets DestroyDataset x

        zpoolMap <- Map.add x.POOL_UID updatedPool zpoolMap
      | ZedPoolProperty x ->
        let updatedPool =
          match Map.tryFind x.POOL_UID zpoolMap with
            | Some pool ->
              { pool with PROPERTIES = pool.PROPERTIES.Add (x.PROPERTY_NAME, x.PROPERTY_VALUE) }
            | None -> failwith (sprintf "Pool to update property on is missing! %A" x.POOL_UID)

        zpoolMap <- Map.add x.POOL_UID updatedPool zpoolMap
      | ZedDatasetProperty x ->
        let updatedDataset (datasets:Map<ZfsDatasetUid, ZfsDataset>) =
          match Map.tryFind (Option.get x.DATASET_UID) datasets with
            | Some dataset ->
              { dataset with PROPERTIES = dataset.PROPERTIES.Add (x.PROPERTY_NAME, x.PROPERTY_VALUE) }
            | None
              -> failwith (sprintf "Dataset to update property on is missing! %A (pool %A)" x.DATASET_UID x.POOL_UID)

        let updatedPool =
          match Map.tryFind x.POOL_UID zpoolMap with
            | Some pool ->
              { pool with DATASETS = pool.DATASETS.Add (Option.get x.DATASET_UID, updatedDataset pool.DATASETS)  }
            | None -> failwith (sprintf "Pool to update dataset property on is missing! %A" x.POOL_UID)

        zpoolMap <- Map.add x.POOL_UID updatedPool zpoolMap
      | ZedGeneric -> ()
      | _ ->
        sock.``end`` None
        raise (System.Exception "Handler got a bad match")

  match closeOnWrite with
    | true -> sock.``end`` getState
    | false -> sock.write getState |> ignore
