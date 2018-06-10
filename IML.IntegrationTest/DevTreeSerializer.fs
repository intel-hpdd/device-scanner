// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTest.DevTreeSerializer

open IML.CommonLibrary
open IML.Types.LegacyTypes
open IML.IntegrationTest
open Fable
open SerializerCommon

let private normalizeDevPath (path:string) =
  path
    |> replace
      "/devices/pci0000:00/0000:00:0d.0/ata\d+/host\d+/target\d+:0:0/\d+:0:0:0/(.+)"
      "/devices/pci0000:00/0000:00:0d.0/ataXX/hostXX/targetXX:0:0/XX:0:0:0/$1"
    |> replace
      "/devices/platform/host\d+/session\d+/target\d+:\d:\d/\d+:\d:\d:\d/(.+)"
      "/devices/platform/hostXX/sessionXX/targetXX:0:0/XX:0:0:0/$1"

let private normalizeParents ((key:string), (blockDev:LegacyDev)) =
  match blockDev with
    | LegacyBlockDev blockDev ->
      (key, {
        blockDev with
          parent = Option.map normalizeDevPath blockDev.parent
      } |> LegacyBlockDev)
    | LegacyZFSDev blockDev -> (key, blockDev |> LegacyZFSDev)

let private normalizeLvUUIDs ((key:string), (blockDev:LegacyDev)) =
  match blockDev with
  | LegacyBlockDev blockDev ->
    (key, {
      blockDev with
        lv_uuid = Option.map (k "XXXXX") blockDev.lv_uuid
    } |> LegacyBlockDev)
  | LegacyZFSDev blockDev -> (key, blockDev |> LegacyZFSDev)

let private normalizeVgUUIDs ((key:string), (blockDev:LegacyDev)) =
  match blockDev with
  | LegacyBlockDev blockDev ->
    (key, {
      blockDev with
        vg_uuid= Option.map (k "XXXXX") blockDev.vg_uuid
    } |> LegacyBlockDev)
  | LegacyZFSDev blockDev -> (key, blockDev |> LegacyZFSDev)

let private normalizeDmSlaveMms ((key:string), (blockDev:LegacyDev)) =
  match blockDev with
  | LegacyBlockDev blockDev ->
    let newDmSlaves =
      blockDev.dm_slave_mms
        |> Array.map (k "xx:yy")

    (key, {blockDev with dm_slave_mms = newDmSlaves} |> LegacyBlockDev)
  | LegacyZFSDev blockDev -> (key, blockDev |> LegacyZFSDev)

let normalizeLegacyBlockDevPaths ((key:string), (blockDev:LegacyDev)) =
  match blockDev with
    | LegacyBlockDev dev ->
      let newPaths =
        dev.paths
          |> Array.map (
            normalizeByUUIDPath
              >> normalizeByLVMUUID
              >> normalizeByDmUUUID
              >> normalizeByPartUUID
              >> normalizeByIp
              >> normalizeByMdUUID
              >> normalizeByMdName
          )
      (key, {dev with paths = newPaths} |> LegacyBlockDev)
    | LegacyZFSDev x -> (key, x |> LegacyZFSDev)

let serialize (x:LegacyDevTree) =
  let devs =
    x.devs
      |> Map.toList
      |> List.map(
        normalizeLegacyBlockDevPaths
        >> normalizeParents
        >> normalizeLvUUIDs
        >> normalizeVgUUIDs
        >> normalizeDmSlaveMms
      )
      |> List.sort
      |> Map.ofList

  let newLvs =
    x.lvs
      |> Map.map (fun _ (table:Map<string, Lv>) ->
        table
          |> Map.map (fun _ (v:Lv) ->
            {v with uuid = "XXXXX"}
          )
      )

  let newVgs =
    x.vgs
      |> Map.map (fun _ (vg:Vg) ->
        {vg with uuid = "XXXXX"}
      )

  {x with
    devs = devs
    lvs = newLvs
    vgs = newVgs
  }
