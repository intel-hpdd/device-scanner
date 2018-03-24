// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Mount

open IML.Types.CommandTypes
// open JsInterop
// open Thot.Json.Decode


type Data = {
  /// mount point
  target: Mount.MountPoint;
  /// mounted block device
  source: Mount.BdevPath; //Mount.Bdev;
  /// filesystem type
  fstype: Mount.FsType;
  /// mount options
  options: Mount.Options;
}

let create target source fstype options =
  {
    target = target;
    source = source;
    fstype = fstype;
    options = options
  }

type LocalMounts = Map<Mount.MountPoint, Data>

let update (localMounts:LocalMounts) (x:MountCommand):Result<LocalMounts, exn> =
  match x with
    | Mount (target, source, fstype, options)
    | Remount (target, source, fstype, options)
    | Move (target, source, fstype, options) ->
      Map.add target (create target source fstype options) localMounts
        |> Ok
    | Umount target ->
      Map.remove target localMounts
        |> Ok
