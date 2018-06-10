// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTest.UdevSerializer

open IML.Types.UeventTypes
open IML.CommonLibrary
open SerializerCommon

let private normalizeDevPath (DevPath(path)) =
  path
    |> replace
      "/devices/pci0000:00/0000:00:0d.0/ata\d+/host\d+/target\d+:0:0/\d+:0:0:0/(.+)"
      "/devices/pci0000:00/0000:00:0d.0/ataXX/hostXX/targetXX:0:0/XX:0:0:0/$1"
    |> replace
      "/devices/platform/host\d+/session\d+/target\d+:\d:\d/\d+:\d:\d:\d/(.+)"
      "/devices/platform/hostXX/sessionXX/targetXX:0:0/XX:0:0:0/$1"
    |> DevPath

let private normalizeDevPaths ((devPath:DevPath), (uevent:UEvent)) =
  let newDevPath = normalizeDevPath devPath
  (newDevPath, {uevent with devpath = newDevPath})

let private normalizeParents (devPath, (uevent:UEvent)) =
  (devPath, {
    uevent with
      parent = (Option.map normalizeDevPath uevent.parent)
  })

let private normalizePaths ((devPath:DevPath), (uevent:UEvent)) =
  let newPaths =
    uevent.paths
      |> Array.map (
        normalizeByUUIDPath
          >> normalizeByLVMUUID
          >> normalizeByDmUUUID
          >> normalizeByPartUUID
          >> normalizeByIp
          >> normalizeByMdUUID
          >> normalizeByMdName
      )

  (devPath, {uevent with paths = newPaths})

let private normalizeLvUUIDs ((devPath:DevPath), (uevent:UEvent)) =
  (devPath, {
    uevent with
      lvUuid = Option.map (k "XXXXX") uevent.lvUuid
  })

let private normalizeVgUUIDs ((devPath:DevPath), (uevent:UEvent)) =
  (devPath, {
    uevent with
      vgUuid = Option.map (k "XXXXX") uevent.vgUuid
  })

let private normalizeFsUUIDs ((devPath:DevPath), (uevent:UEvent)) =
  let newFsUUID =
    uevent.fsUuid
      |> Option.map (k "uuid-XXXXX")

  (devPath, {uevent with fsUuid = newFsUUID})

let private normalizeDmSlaveMms ((devPath:DevPath), (uevent:UEvent)) =
  let newDmSlaves =
    uevent.dmSlaveMMs
      |> Array.map (k "xx:yy")

  (devPath, {uevent with dmSlaveMMs = newDmSlaves})

let private normalizeMdRaidUuid ((devPath:DevPath), (uevent:UEvent)) =
  let newMdRaidUUID =
    uevent.mdUUID
      |> Option.map (k "aa:bb:cc:dd")

  (devPath, {uevent with mdUUID = newMdRaidUUID})

let serialize (x:Map<DevPath, UEvent>) =
  x
    |> Map.toList
    |> List.map (
        normalizeDevPaths
        >> normalizeParents
        >> normalizePaths
        >> normalizeLvUUIDs
        >> normalizeVgUUIDs
        >> normalizeFsUUIDs
        >> normalizeDmSlaveMms
        >> normalizeMdRaidUuid
      )
    |> List.sort
    |> Map.ofList
