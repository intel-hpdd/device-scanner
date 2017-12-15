// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.ZFSEventTypes

open Fable.PowerPack.Json
open IML.JsonDecoders
open IML.Common

let private hasPair' k v m =
  hasPair k (String v) m

let private tryFindStr = tryFindJson str

let private hasZeventClassName = hasPair' "ZEVENT_CLASS"

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
  ZEVENT_POOL_GUID: ZfsPoolUid;
  ZEVENT_POOL_STATE_STR: string;
  ZEVENT_HISTORY_DSID: ZfsDatasetUid option;
  ZEVENT_HISTORY_DSNAME: string option;
}

type ZedPoolEvent = {
  ZEVENT_EID: string;
  ZED_PID: string;
  ZEVENT_TIME: string;
  ZEVENT_CLASS: string;
  ZEVENT_SUBCLASS: string;
  ZEVENT_POOL: string;
  ZEVENT_POOL_GUID: ZfsPoolUid;
  ZEVENT_POOL_STATE_STR: string;
}

type ZfsProperty = {
  POOL_UID: ZfsPoolUid;
  DATASET_UID: ZfsDatasetUid option;
  PROPERTY_NAME: string;
  PROPERTY_VALUE: string;
}

let private parsePropertyName (x:string) = x.Split([| '=' |]).[0]

let private parsePropertyValue (x:string) = x.Split([| '=' |]).[1]

let private parsePoolUid = findStr "ZEVENT_POOL_GUID" >> ZfsPoolUid

let private parseDatasetUid = tryFindStr "ZEVENT_HISTORY_DSID" >> Option.map ZfsDatasetUid

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
    ZEVENT_POOL_GUID = parsePoolUid x;
    ZEVENT_POOL_STATE_STR = findStr "ZEVENT_POOL_STATE_STR" x;
    ZEVENT_HISTORY_DSID = parseDatasetUid x;
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
    ZEVENT_POOL_GUID = parsePoolUid x;
    ZEVENT_POOL_STATE_STR = findStr "ZEVENT_POOL_STATE_STR" x;
  }

let poolFromEvent x =
  {
    NAME = x.ZEVENT_POOL;
    UID = x.ZEVENT_POOL_GUID;
    STATE_STR = x.ZEVENT_POOL_STATE_STR;
    PATH = x.ZEVENT_POOL;
    DATASETS = Map.empty;
    PROPERTIES = Map.empty;
  }

let datasetFromEvent (x:ZedHistoryEvent) =
  {
    POOL_UID = x.ZEVENT_POOL_GUID;
    DATASET_NAME = Option.get x.ZEVENT_HISTORY_DSNAME;
    DATASET_UID = Option.get x.ZEVENT_HISTORY_DSID;
    PROPERTIES = Map.empty;
  }

let propertyFromEvent (x:ZedHistoryEvent) =
  {
    POOL_UID = x.ZEVENT_POOL_GUID;
    DATASET_UID = x.ZEVENT_HISTORY_DSID;
    PROPERTY_NAME = parsePropertyName x.ZEVENT_HISTORY_INTERNAL_STR;
    PROPERTY_VALUE = parsePropertyValue x.ZEVENT_HISTORY_INTERNAL_STR;
  }

let private mapToPool = extractPoolEvent >> poolFromEvent >> Some

let private mapToDataset = extractHistoryEvent >> datasetFromEvent >> Some

let private mapToProperty = extractHistoryEvent >> propertyFromEvent >> Some

let private isDestroyClass = hasZeventClassName "sysevent.fs.zfs.pool_destroy"

let private isHistoryClass = hasZeventClassName "sysevent.fs.zfs.history_event"

let isSetInternalName = hasPair' "ZEVENT_HISTORY_INTERNAL_NAME" "set"

let (|ZedGeneric|_|) =
  function
    | x when Map.containsKey "ZEVENT_EID" x -> Some ()
    | _ -> None

let (|ZedPool|_|) str =
  function
    | x when hasZeventClassName ("sysevent.fs.zfs.pool_" + str) x -> mapToPool x
    | _ -> None

let (|ZedExport|_|) =
  let isExportState = hasPair' "ZEVENT_POOL_STATE_STR" "EXPORTED"

  function
    | x when isDestroyClass x && isExportState x -> mapToPool x
    | _ -> None

let (|ZedDestroy|_|) =
  let isDestroyState = hasPair' "ZEVENT_POOL_STATE_STR" "DESTROYED"

  function
    | x when isDestroyClass x && isDestroyState x -> mapToPool x
    | _ -> None

let (|ZedDataset|_|) str =
  let isInternalName = hasPair' "ZEVENT_HISTORY_INTERNAL_NAME" str

  function
    | x when Map.containsKey "ZEVENT_HISTORY_DSID" x
             && isHistoryClass x && isInternalName x
      -> mapToDataset x
    | _ -> None

let (|ZedPoolProperty|_|) =
  function
    | x when not (Map.containsKey "ZEVENT_HISTORY_DSID" x)
             && isHistoryClass x && isSetInternalName x
      -> mapToProperty x
    | _ -> None

let (|ZedDatasetProperty|_|) =
  function
    | x when Map.containsKey "ZEVENT_HISTORY_DSID" x
             && isHistoryClass x && isSetInternalName x
      -> mapToProperty x
    | _ -> None
