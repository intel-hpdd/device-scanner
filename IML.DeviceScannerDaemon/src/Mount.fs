// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Mount

open IML.Types.CommandTypes
open IML.Types.MountTypes


let update (localMounts:LocalMounts) (x:MountCommand):Result<LocalMounts, exn> =
  match x with
    // fixme: remove old entry matching "source", add new entry
    // | AddMount _
    // ???
    // | ReplaceMount _ ->
      // localMounts
    | AddMount (Mount.MountPoint target, Mount.BdevPath source, Mount.FsType fstype, Mount.MountOpts opts) ->
      Set.add (
        {
          target = target;
          source = source;
          fstype = fstype;
          opts = opts;
        }) localMounts
    | RemoveMount (Mount.MountPoint target, Mount.BdevPath source, Mount.FsType fstype, Mount.MountOpts opts) ->
      Set.remove (
        {
          target = target;
          source = source;
          fstype = fstype;
          opts = opts;
        }) localMounts
  |> Ok
