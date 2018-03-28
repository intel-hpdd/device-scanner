// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Mount

open IML.Types.CommandTypes


type LocalMounts = Set<MountData> // Map<Mount.MountPoint, Mount.Data>

let update (localMounts:LocalMounts) (x:MountCommand):Result<LocalMounts, exn> =
  match x with
    | Mount y
    | Remount y
    | Move y ->
      Set.add y localMounts
        |> Ok
    | Umount y ->
      Set.remove y localMounts
        |> Ok
