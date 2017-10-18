// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.ZFSEventTypes

open System.Collections.Generic
open Fable.Core
open Fable.PowerPack
open JsonDecoders

/// The Event IDentifier.
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
  | x -> raise (System.Exception ("Could not match ZeventPoolStateStr, got" + x))

type ZedGenericEvent = {
  ZEVENT_EID: ZeventEid;
  ZED_PID: ZedPid;
  ZEVENT_TIME: ZeventTime;
  ZEVENT_CLASS: ZeventClass;
  ZEVENT_SUBCLASS: ZeventSubclass;
}

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
  DATASETS: Dictionary<ZeventGuid, ZfsDataset>;
}
  // SIZE: ZeventSize;

let private historyClass = ZeventClass("sysevent.fs.zfs.history_event")

let private poolClassPrefix = "sysevent.fs.zfs.pool_"

let poolCreateClass = ZeventClass("sysevent.fs.zfs.pool_create")

let poolDestroyClass = ZeventClass("sysevent.fs.zfs.pool_destroy")

let private poolImportClass = ZeventClass("sysevent.fs.zfs.pool_import")

let private poolDestroyedState = matchZeventPoolStateStr("DESTROYED")

let private poolExportedState = matchZeventPoolStateStr("EXPORTED")

let private datasetCreateName = ZeventHistoryInternalName("create")

let private datasetDestroyName = ZeventHistoryInternalName("destroy")

let private matchClassName name x =
  x
  |> Map.tryFind "ZEVENT_CLASS"
  |> Option.bind str
  |> Option.map ZeventClass
  |> Option.filter((=) name)
  |> Option.map(fun _ -> x)

let private matchClassNameStartswith name x =
  x
  |> Map.tryFind "ZEVENT_CLASS"
  |> Option.bind str
  |> Option.filter(fun x -> x.StartsWith(name))
  |> Option.map(fun _ -> x)

let private matchStateStr state x =
  x
  |> Map.tryFind "ZEVENT_POOL_STATE_STR"
  |> Option.bind str
  |> Option.map matchZeventPoolStateStr
  |> Option.filter((=) state)
  |> Option.map(fun _ -> x)

let private matchZeventIdExists x =
  x
  |> Map.tryFind "ZEVENT_EID"
  |> Option.bind str
  |> Option.map ZeventEid
  |> Option.map(fun _ -> x)

let private matchDatasetIdExists x =
  x
  |> Map.tryFind "ZEVENT_HISTORY_DSID"
  |> Option.bind str
  |> Option.map ZeventHistoryDsid
  |> Option.map(fun _ -> x)

let private matchInternalName name x =
  x
  |> Map.tryFind "ZEVENT_HISTORY_INTERNAL_NAME"
  |> Option.bind str
  |> Option.map ZeventHistoryInternalName
  |> Option.filter((=) name)
  |> Option.map(fun _ -> x)

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

let private parseZeventPoolSize = findOrNone "ZEVENT_POOL_SIZE" >> Option.map ZeventSize

let private parseZeventPoolStateStr = findOrFail "ZEVENT_POOL_STATE_STR" >> matchZeventPoolStateStr

let private parseZeventHistoryDsid = findOrNone "ZEVENT_HISTORY_DSID" >> Option.map ZeventGuid

let private parseZeventHistoryDsName = findOrNone "ZEVENT_HISTORY_DSNAME" >> Option.map ZeventName

let private parseZeventDataset = findOrFail "ZEVENT_HISTORY_DSNAME" >> ZeventName

let private parseZeventDatasetGuid = findOrFail "ZEVENT_HISTORY_DSNAME" >> ZeventGuid

let private parseZeventDatasetSize = findOrNone "ZEVENT_DATASET_SIZE" >> Option.map ZeventSize

let (|ZedGenericEventMatch|_|) (x:Json.Json) =
  x
    |> object
    |> Option.bind(matchZeventIdExists)
    |> Option.map(fun x ->
      {
        ZEVENT_EID = parseZeventEid x;
        ZED_PID = parseZedPid x;
        ZEVENT_TIME = parseZeventTime x;
        ZEVENT_CLASS = parseZeventClass x;
        ZEVENT_SUBCLASS = parseZeventSubclass x;
      }
    )

let (|ZedPoolEventMatch|_|) (x:Json.Json) =
  x
    |> object
    |> Option.bind(matchClassNameStartswith poolClassPrefix)
    |> Option.map(fun x ->
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
    )

let (|ZedHistoryEventMatch|_|) (x:Json.Json) =
  x
    |> object
    |> Option.bind(matchClassName historyClass)
    |> Option.map(fun x ->
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
    )

let (|ZedPoolCreateEventMatch|_|) (x:Json.Json) =
  x
    |> object
    |> Option.bind(matchClassName poolCreateClass)
    |> Option.map(fun x ->
      {
        NAME = parseZeventPool x;
        UID = parseZeventPoolGuid x;
        STATE_STR = parseZeventPoolStateStr x;
        PATH = parseZeventPool x;
        DATASETS = Dictionary();
      }
    )

let (|ZedPoolDestroyEventMatch|_|) (x:Json.Json) =
  x
    |> object
    |> Option.bind(matchClassName poolDestroyClass)
    |> Option.bind(matchStateStr poolDestroyedState)
    |> Option.map(fun x ->
      {
        NAME = parseZeventPool x;
        UID = parseZeventPoolGuid x;
        STATE_STR = parseZeventPoolStateStr x;
        PATH = parseZeventPool x;
        DATASETS = Dictionary();
      }
    )

let (|ZedPoolExportEventMatch|_|) (x:Json.Json) =
  x
    |> object
    |> Option.bind(matchClassName poolDestroyClass)
    |> Option.bind(matchStateStr poolExportedState)
    |> Option.map(fun x ->
      {
        NAME = parseZeventPool x;
        UID = parseZeventPoolGuid x;
        STATE_STR = parseZeventPoolStateStr x;
        PATH = parseZeventPool x;
        DATASETS = Dictionary();
      }
    )

let (|ZedPoolImportEventMatch|_|) (x:Json.Json) =
  x
    |> object
    |> Option.bind(matchClassName poolImportClass)
    |> Option.map(fun x ->
      {
        NAME = parseZeventPool x;
        UID = parseZeventPoolGuid x;
        STATE_STR = parseZeventPoolStateStr x;
        PATH = parseZeventPool x;
        DATASETS = Dictionary();
      }
    )

let (|ZedDatasetCreateEventMatch|_|) (x:Json.Json) =
  x
    |> object
    |> Option.bind(matchClassName historyClass)
    |> Option.bind(matchDatasetIdExists)
    |> Option.bind(matchInternalName datasetCreateName)
    |> Option.map(fun x ->
      {
        POOL_UID = parseZeventPoolGuid x;
        DATASET_UID = parseZeventDatasetGuid x;
        DATASET_NAME = parseZeventDataset x;
      }
    )

let (|ZedDatasetDestroyEventMatch|_|) (x:Json.Json) =
  x
    |> object
    |> Option.bind(matchClassName historyClass)
    |> Option.bind(matchDatasetIdExists)
    |> Option.bind(matchInternalName datasetDestroyName)
    |> Option.map(fun x ->
      {
        POOL_UID = parseZeventPoolGuid x;
        DATASET_UID = parseZeventDatasetGuid x;
        DATASET_NAME = parseZeventDataset x;
      }
    )
