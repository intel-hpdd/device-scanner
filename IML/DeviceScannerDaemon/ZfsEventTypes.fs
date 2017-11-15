// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.ZFSEventTypes

open Fable.PowerPack.Json
open IML.JsonDecoders

let private hasPair k v m =
  m
    |> Map.tryFindKey (fun k' v' -> k = k' && String(v) = v')
    |> Option.isSome

let private tryFindStr = tryFindJson str

let private hasZeventClassName = hasPair "ZEVENT_CLASS"

type ZedHistoryEvent = {
  ZEVENT_EID: string;
  ZED_PID: string;
  /// The time at which the zevent was posted as "seconds nanoseconds" since the Epoch.
  ZEVENT_TIME: string;
  ZEVENT_CLASS: string;
  ZEVENT_SUBCLASS: string;
  ZEVENT_HISTORY_HOSTNAME: string;
  ZEVENT_HISTORY_INTERNAL_NAME: string;
  ZEVENT_HISTORY_INTERNAL_STR: string;
  ZEVENT_POOL: string;
  ZEVENT_POOL_GUID: string;
  ZEVENT_POOL_STATE_STR: string;
  ZEVENT_HISTORY_DSID: string option;
  ZEVENT_HISTORY_DSNAME: string option;
}

type ZedPoolEvent = {
  ZEVENT_EID: string;
  ZED_PID: string;
  ZEVENT_TIME: string;
  ZEVENT_CLASS: string;
  ZEVENT_SUBCLASS: string;
  ZEVENT_POOL: string;
  ZEVENT_POOL_GUID: string;
  ZEVENT_POOL_STATE_STR: string;
}

type ZfsDataset = {
  POOL_UID: string;
  DATASET_NAME: string;
  DATASET_UID: string;
}

type ZfsPool = {
  NAME: string;
  UID: string;
  STATE_STR: string;
  PATH: string;
  DATASETS: Map<string, ZfsDataset>;
}

let extractHistoryEvent x =
  {
    ZEVENT_EID = findStr "ZEVENT_EID" x;
    ZED_PID = findStr "ZED_PID" x;
    ZEVENT_TIME = findStr "ZEVENT_TIME" x;
    ZEVENT_CLASS = findStr "ZEVENT_CLASS" x;
    ZEVENT_SUBCLASS = findStr "ZEVENT_SUBCLASS" x;
    ZEVENT_HISTORY_HOSTNAME = findStr "ZEVENT_HISTORY_HOSTNAME" x;
    ZEVENT_HISTORY_INTERNAL_NAME = findStr "ZEVENT_HISTORY_INTERNAL_NAME" x;
    ZEVENT_HISTORY_INTERNAL_STR = findStr "ZEVENT_HISTORY_INTERNAL_STR" x;
    ZEVENT_POOL = findStr "ZEVENT_POOL" x;
    ZEVENT_POOL_GUID = findStr "ZEVENT_POOL_GUID" x;
    ZEVENT_POOL_STATE_STR = findStr "ZEVENT_POOL_STATE_STR" x;
    ZEVENT_HISTORY_DSID = tryFindStr "ZEVENT_HISTORY_DSID" x;
    ZEVENT_HISTORY_DSNAME = tryFindStr "ZEVENT_HISTORY_DSNAME" x;
  }

let extractPoolEvent x =
  {
    ZEVENT_EID = findStr "ZEVENT_EID" x;
    ZED_PID = findStr "ZED_PID" x;
    ZEVENT_TIME = findStr "ZEVENT_TIME" x;
    ZEVENT_CLASS = findStr "ZEVENT_CLASS" x;
    ZEVENT_SUBCLASS = findStr "ZEVENT_SUBCLASS" x;
    ZEVENT_POOL = findStr "ZEVENT_POOL" x;
    ZEVENT_POOL_GUID = findStr "ZEVENT_POOL_GUID" x;
    ZEVENT_POOL_STATE_STR = findStr "ZEVENT_POOL_STATE_STR" x;
  }

let poolFromEvent x =
  {
    NAME = x.ZEVENT_POOL;
    UID = x.ZEVENT_POOL_GUID;
    STATE_STR = x.ZEVENT_POOL_STATE_STR;
    PATH = x.ZEVENT_POOL;
    DATASETS = Map.empty;
  }

let datasetFromEvent (x:ZedHistoryEvent) =
  {
    POOL_UID = x.ZEVENT_POOL_GUID;
    DATASET_NAME = Option.get x.ZEVENT_HISTORY_DSNAME;
    DATASET_UID = Option.get x.ZEVENT_HISTORY_DSID;
  }

let private mapToPool = extractPoolEvent >> poolFromEvent >> Some

let private mapToDataset = extractHistoryEvent >> datasetFromEvent >> Some

let private isDestroyClass = hasZeventClassName "sysevent.fs.zfs.pool_destroy"

let private isHistoryClass = hasZeventClassName "sysevent.fs.zfs.history_event"

let (|ZedGeneric|_|) =
  function
    | x when Map.containsKey "ZEVENT_EID" x -> Some ()
    | _ -> None

let (|ZedPool|_|) str =
  function
    | x when hasZeventClassName ("sysevent.fs.zfs.pool_" + str) x -> mapToPool x
    | _ -> None

let (|ZedExport|_|) =
  let isExportState = hasPair "ZEVENT_POOL_STATE_STR" "EXPORTED"

  function
    | x when isDestroyClass x && isExportState x -> mapToPool x
    | _ -> None

let (|ZedDestroy|_|) =
  let isDestroyState = hasPair "ZEVENT_POOL_STATE_STR" "DESTROYED"

  function
    | x when isDestroyClass x && isDestroyState x -> mapToPool x
    | _ -> None

let (|ZedDataset|_|) str =
  let isInternalName = hasPair "ZEVENT_HISTORY_INTERNAL_NAME" str

  function
    | x when isHistoryClass x && isInternalName x -> mapToDataset x
    | _ -> None
