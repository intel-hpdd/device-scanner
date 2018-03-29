// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Mount

open IML.Types.CommandTypes


type LocalMounts = Set<MountData> // Map<Mount.MountPoint, Mount.Data>

let update (localMounts:LocalMounts) (x:MountCommand):Result<LocalMounts, exn> =
  match x with
    // fixme: remove old entry matching "source", add new entry
    | Move _
    // ???
    | Remount _ ->
      localMounts
    | Mount y ->
      Set.add y localMounts
    | Umount y ->
      Set.remove y localMounts
  |> Ok
