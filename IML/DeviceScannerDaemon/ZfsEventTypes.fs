// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.ZFSEventTypes

open Fable.Core
open JsonDecoders
open Fable.PowerPack.Json

/// The Event Identifier.
[<Erase>]
type ZeventEid = ZeventEid of string

/// The daemon's process ID.
[<Erase>]
type ZedPid = ZedPid of string

/// The ZFS Version emitting this event.
[<Erase>]
type ZfsVersion = ZfsVersion of string

/// The zevent class string.
[<Erase>]
type ZeventClass = ZeventClass of string

/// The zevent subclass string.
[<Erase>]
type ZeventSubclass = ZeventSubclass of string

/// The name of the Zfs object the Zevent occurred on.
[<Erase>]
type ZeventName = ZeventName of string

/// The GUID of the Zfs object the Zevent occurred on.
[<Erase>]
type ZeventGuid = ZeventGuid of string

/// The size of the Zfs object the Zevent occurred on.
[<Erase>]
type ZeventSize = ZeventSize of string

type ZeventHistoryDsid = ZeventHistoryDsid of string

type ZeventHistoryDsName = ZeventHistoryDsName of string

/// The time at which the zevent was posted as "seconds nanoseconds" since the Epoch.
[<Erase>]
type ZeventTime = ZeventTime of string

[<Erase>]
type ZeventHistoryHostname = ZeventHistoryHostname of string

[<Erase>]
type ZeventHistoryInternalName = ZeventHistoryInternalName of string

[<Erase>]
type ZeventHistoryInternalStr = ZeventHistoryInternalStr of string

[<StringEnum>]
type ZeventPoolStateStr =
  | [<CompiledName("ACTIVE")>] Active /// In active use
  | [<CompiledName("EXPORTED")>] Exported /// Explicitly exported
  | [<CompiledName("DESTROYED")>] Destroyed /// Explicitly destroyed
  | [<CompiledName("SPARE")>] Spare /// Reserved for hot spare use
  | [<CompiledName("L2CACHE")>] L2Cache /// Level 2 ARC device
  | [<CompiledName("UNINITIALIZED")>] Uninitialized /// Internal spa_t state
  | [<CompiledName("UNAVAIL")>] Unavail /// Internal libzfs state
  | [<CompiledName("POTENTIALLY_ACTIVE")>] PotentiallyActive /// Internal libzfs state

let private matchZeventPoolStateStr = function
  | "ACTIVE" -> Active
  | "EXPORTED" -> Exported
  | "DESTROYED" -> Destroyed
  | "SPARE" -> Spare
  | "L2CACHE" -> L2Cache
  | "UNINITIALIZED" -> Uninitialized
  | "UNAVAIL" -> Unavail
  | "POTENTIALLY_ACTIVE" -> PotentiallyActive
  | x -> failwith ("Could not match ZeventPoolStateStr, got" + x)

let private hasPair k v m =
  m
    |> Map.tryFindKey (fun k' v' -> k = k' && String(v) = v')
    |> Option.isSome

let private hasZeventClassName = hasPair "ZEVENT_CLASS"

type ZedHistoryEvent = {
  ZEVENT_EID: ZeventEid;
  ZED_PID: ZedPid;
  ZEVENT_TIME: ZeventTime;
  ZEVENT_CLASS: ZeventClass;
  ZEVENT_SUBCLASS: ZeventSubclass;
  ZEVENT_HISTORY_HOSTNAME: ZeventHistoryHostname;
  ZEVENT_HISTORY_INTERNAL_NAME: ZeventHistoryInternalName;
  ZEVENT_HISTORY_INTERNAL_STR: ZeventHistoryInternalStr;
  ZEVENT_POOL: ZeventName;
  ZEVENT_POOL_GUID: ZeventGuid;
  ZEVENT_POOL_STATE_STR: ZeventPoolStateStr;
  ZEVENT_HISTORY_DSID: ZeventGuid option;
  ZEVENT_HISTORY_DSNAME: ZeventName option;
}

type ZedPoolEvent = {
  ZEVENT_EID: ZeventEid;
  ZED_PID: ZedPid;
  ZEVENT_TIME: ZeventTime;
  ZEVENT_CLASS: ZeventClass;
  ZEVENT_SUBCLASS: ZeventSubclass;
  ZEVENT_POOL: ZeventName;
  ZEVENT_POOL_GUID: ZeventGuid;
  ZEVENT_POOL_STATE_STR: ZeventPoolStateStr;
}

type ZfsDataset = {
  POOL_UID: ZeventGuid;
  DATASET_NAME: ZeventName;
  DATASET_UID: ZeventGuid;
}
  // SIZE: ZeventSize;

type ZfsPool = {
  NAME: ZeventName;
  UID: ZeventGuid;
  STATE_STR: ZeventPoolStateStr;
  PATH: ZeventName;
  DATASETS: Map<ZeventGuid, ZfsDataset>;
}
  // SIZE: ZeventSize;

let poolCreateClass = ZeventClass("sysevent.fs.zfs.pool_create")

let poolDestroyClass = ZeventClass("sysevent.fs.zfs.pool_destroy")

let private parseZeventEid = findOrFail "ZEVENT_EID" >> ZeventEid

let private parseZedPid = findOrFail "ZED_PID" >> ZedPid

let private parseZeventTime = findOrFail "ZEVENT_TIME" >> ZeventTime

let private parseZeventClass = findOrFail "ZEVENT_CLASS" >> ZeventClass

let private parseZeventSubclass = findOrFail "ZEVENT_SUBCLASS" >> ZeventSubclass

let private parseZeventHistoryHostname = findOrFail "ZEVENT_HISTORY_HOSTNAME" >> ZeventHistoryHostname

let private parseZeventHistoryInternalName = findOrFail "ZEVENT_HISTORY_INTERNAL_NAME" >> ZeventHistoryInternalName

let private parseZeventHistoryInternalStr = findOrFail "ZEVENT_HISTORY_INTERNAL_STR" >> ZeventHistoryInternalStr

let private parseZeventPool = findOrFail "ZEVENT_POOL" >> ZeventName

let private parseZeventPoolGuid = findOrFail "ZEVENT_POOL_GUID" >> ZeventGuid

// let private parseZeventPoolSize = findOrNone "ZEVENT_POOL_SIZE" >> Option.map ZeventSize

let private parseZeventPoolStateStr = findOrFail "ZEVENT_POOL_STATE_STR" >> matchZeventPoolStateStr

let private parseZeventHistoryDsid = findOrNone "ZEVENT_HISTORY_DSID" >> Option.map ZeventGuid

let private parseZeventHistoryDsName = findOrNone "ZEVENT_HISTORY_DSNAME" >> Option.map ZeventName

// let private parseZeventDatasetSize = findOrNone "ZEVENT_DATASET_SIZE" >> Option.map ZeventSize

let (|ZedGeneric|_|) =
  function
    | x when Map.containsKey "ZEVENT_EID" x -> Some ()
    | _ -> None

let extractPoolEvent x =
  {
    ZEVENT_EID = parseZeventEid x;
    ZED_PID = parseZedPid x;
    ZEVENT_TIME = parseZeventTime x;
    ZEVENT_CLASS = parseZeventClass x;
    ZEVENT_SUBCLASS = parseZeventSubclass x;
    ZEVENT_POOL = parseZeventPool x;
    ZEVENT_POOL_GUID = parseZeventPoolGuid x;
    ZEVENT_POOL_STATE_STR = parseZeventPoolStateStr x;
  }

let poolFromEvent x =
  {
    NAME = x.ZEVENT_POOL;
    UID = x.ZEVENT_POOL_GUID;
    STATE_STR = x.ZEVENT_POOL_STATE_STR;
    PATH = x.ZEVENT_POOL;
    DATASETS = Map.empty;
  }

let private mapToPool = extractPoolEvent >> poolFromEvent >> Some

let (|ZedPool|_|) str =
  function
    | x when hasZeventClassName ("sysevent.fs.zfs.pool_" + str) x -> mapToPool x
    | _ -> None

let private isDestroyClass = hasZeventClassName "sysevent.fs.zfs.pool_destroy"

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

let extractHistoryEvent x =
  {
    ZEVENT_EID = parseZeventEid x;
    ZED_PID = parseZedPid x;
    ZEVENT_TIME = parseZeventTime x;
    ZEVENT_CLASS = parseZeventClass x;
    ZEVENT_SUBCLASS = parseZeventSubclass x;
    ZEVENT_HISTORY_HOSTNAME = parseZeventHistoryHostname x;
    ZEVENT_HISTORY_INTERNAL_NAME = parseZeventHistoryInternalName x;
    ZEVENT_HISTORY_INTERNAL_STR = parseZeventHistoryInternalStr x;
    ZEVENT_POOL = parseZeventPool x;
    ZEVENT_POOL_GUID = parseZeventPoolGuid x;
    ZEVENT_POOL_STATE_STR = parseZeventPoolStateStr x;
    ZEVENT_HISTORY_DSID = parseZeventHistoryDsid x;
    ZEVENT_HISTORY_DSNAME = parseZeventHistoryDsName x;
  }

let datasetFromEvent (x:ZedHistoryEvent) =
  {
    POOL_UID = x.ZEVENT_POOL_GUID;
    DATASET_NAME = Option.get x.ZEVENT_HISTORY_DSNAME;
    DATASET_UID = Option.get x.ZEVENT_HISTORY_DSID;
  }

let private mapToDataset = extractHistoryEvent >> datasetFromEvent >> Some

let private isHistoryClass = hasZeventClassName "sysevent.fs.zfs.history_event"

let (|ZedHistory|_|) =
  function
    | x when hasZeventClassName "sysevent.fs.zfs.history_event" x -> Some (extractHistoryEvent x)
    | _ -> None

let (|ZedDataset|_|) str =
  let isInternalName = hasPair "ZEVENT_HISTORY_INTERNAL_NAME" str

  function
    | x when isHistoryClass x && isInternalName x -> mapToDataset x
    | _ -> None
