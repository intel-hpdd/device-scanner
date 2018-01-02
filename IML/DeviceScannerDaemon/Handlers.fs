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

let mutable devices:Map<DevPath, AddEvent> = Map.empty
let mutable zpools:Map<ZfsPoolUid, ZfsPool> = Map.empty
// let mutable connections:Map<ConnId, string> = Map.empty

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

  match Map.tryFind x.POOL_UID zpools with
    | Some pool ->
      { pool with DATASETS = matchAction pool }
    | None -> failwith (sprintf "Pool to update dataset on is missing! %A" x.POOL_UID)

let mutable private shouldEnd = true

let private getState () =
  { BLOCK_DEVICES = devices; ZFSPOOLS = zpools }
    |> toJson
    |> Some

let dataHandler (sock:Net.Socket) x =
  x
    |> unwrapObject
    |> function
      | Info -> (shouldEnd <- true)
      | Stream -> (shouldEnd <- false)
      | UdevAdd x | UdevChange x -> (devices <- Map.add x.DEVPATH x devices)
      | UdevRemove x -> (devices <- Map.remove x devices)
      | ZedPool "create" x -> (zpools <- Map.add x.UID x zpools)
      | ZedPool "import" x | ZedExport x ->
        let updatedPool =
          match Map.tryFind x.UID zpools with
            | Some pool ->
              { x with DATASETS = pool.DATASETS; PROPERTIES = pool.PROPERTIES }
            | None -> x

        zpools <- Map.add x.UID updatedPool zpools
      | ZedDestroy x -> (zpools <- zpools.Remove x.UID)
      | ZedDataset "create" x ->
        let updatedPool = updateDatasets CreateDataset x

        zpools <- Map.add x.POOL_UID updatedPool zpools
      | ZedDataset "destroy" x ->
        let updatedPool = updateDatasets DestroyDataset x

        zpools <- Map.add x.POOL_UID updatedPool zpools
      | ZedPoolProperty x ->
        let updatedPool =
          match Map.tryFind x.POOL_UID zpools with
            | Some pool ->
              { pool with PROPERTIES = pool.PROPERTIES.Add (x.PROPERTY_NAME, x.PROPERTY_VALUE) }
            | None -> failwith (sprintf "Pool to update property on is missing! %A" x.POOL_UID)

        zpools <- Map.add x.POOL_UID updatedPool zpools
      | ZedDatasetProperty x ->
        let updatedDataset (datasets:Map<ZfsDatasetUid, ZfsDataset>) =
          match Map.tryFind (Option.get x.DATASET_UID) datasets with
            | Some dataset ->
              { dataset with PROPERTIES = dataset.PROPERTIES.Add (x.PROPERTY_NAME, x.PROPERTY_VALUE) }
            | None
              -> failwith (sprintf "Dataset to update property on is missing! %A (pool %A)" x.DATASET_UID x.POOL_UID)

        let updatedPool =
          match Map.tryFind x.POOL_UID zpools with
            | Some pool ->
              { pool with DATASETS = pool.DATASETS.Add (Option.get x.DATASET_UID, updatedDataset pool.DATASETS)  }
            | None -> failwith (sprintf "Pool to update dataset property on is missing! %A" x.POOL_UID)

        zpools <- Map.add x.POOL_UID updatedPool zpools
      | ZedGeneric -> ()
      | _ ->
        sock.``end`` None
        raise (System.Exception "Handler got a bad match")

  match shouldEnd with
    | true ->
      System.Console.Write "shouldEnd=true, ending with state"
      sock.``end`` (getState ())
    | false ->
      System.Console.Write "shouldEnd=false, writing state"
      sock.write (getState ()) |> ignore

