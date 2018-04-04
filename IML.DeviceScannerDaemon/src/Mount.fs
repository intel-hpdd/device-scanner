// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Mount

open IML.Types.CommandTypes
open MountTypes


let update (localMounts:LocalMounts) (x:MountCommand):Result<LocalMounts, exn> =
  match x with
    // fixme: remove old entry matching "source", add new entry
    // | AddMount _
    // ???
    // | ReplaceMount _ ->
      // localMounts
    | AddMount (target, source, fstype, opts) ->
      Set.add (LocalMount (target, source, fstype, opts)) localMounts
    | RemoveMount (target, source, fstype, opts) ->
      Set.remove (LocalMount (target, source, fstype, opts)) localMounts
  |> Ok
