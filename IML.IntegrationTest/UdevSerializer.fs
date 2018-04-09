// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTest.UdevSerializer

open IML.Types.UeventTypes
open System.Text.RegularExpressions

let private k p _ =
  p

let private normalizeDevPath (DevPath(path)) =
  Regex.Replace (
    path,
    "/devices/pci0000:00/0000:00:0d.0/ata\d+/host\d+/target\d+:0:0/\d+:0:0:0/(.+)",
    "/devices/pci0000:00/0000:00:0d.0/ataXX/hostXX/targetXX:0:0/XX:0:0:0/$1"
  )
    |> DevPath

let private normalizeByPath (regex:string) (replacement:string) (Path(path)) =
  Regex.Replace (
    path,
    regex,
    sprintf "$1%s" replacement
  )
    |> Path

let private normalizeByUUIDPath (path:Path) =
  normalizeByPath "(/dev/disk/by-uuid/).+" "uuid-XXXXX" path

let private normalizeByLVMUUID (path:Path) =
  normalizeByPath "(/dev/disk/by-id/)lvm-pv-uuid-.+" "lvm-pv-uuid-XXXXX" path

let private normalizeByDmUUUID (path:Path) =
  normalizeByPath "(/dev/disk/by-id/)dm-uuid-LVM-.+" "dm-uuid-LVM-XXXXX" path

let private normalizeDevPaths ((devPath:DevPath), (uevent:UEvent)) =
  let newDevPath = normalizeDevPath devPath
  (newDevPath, {uevent with devpath = newDevPath})

let private normalizeByPartUUID (path:Path) =
  normalizeByPath "(/dev/disk/by-partuuid/).+" "part-uuid-XXXXX" path

let private normalizePaths ((devPath:DevPath), (uevent:UEvent)) =
  let newPaths =
    uevent.paths
      |> Array.map (normalizeByUUIDPath >> normalizeByLVMUUID >> normalizeByDmUUUID >> normalizeByPartUUID)

  (devPath, {uevent with paths = newPaths})

let private normalizeDMUUIDs ((devPath:DevPath), (uevent:UEvent)) =
  let newDmUUID =
    uevent.dmUUID
      |> Option.map (k "LVM-lvmXXXXX")

  (devPath, {uevent with dmUUID = newDmUUID})

let private normalizeFsUUIDs ((devPath:DevPath), (uevent:UEvent)) =
  let newFsUUID =
    uevent.fsUuid
      |> Option.map (k "uuid-XXXXX")

  (devPath, {uevent with fsUuid = newFsUUID})

let serialize (x:Map<DevPath, UEvent>) =
  x
    |> Map.toList
    |> List.map (normalizeDevPaths >> normalizePaths >> normalizeDMUUIDs >> normalizeFsUUIDs)
    |> List.sort
    |> Map.ofList
