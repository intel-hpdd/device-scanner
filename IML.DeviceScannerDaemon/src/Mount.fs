// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.
module IML.DeviceScannerDaemon.Mount

open IML.Types.CommandTypes
open IML.Types.MountTypes

let update (localMounts : LocalMounts) (x : MountCommand) : Result<LocalMounts, exn> =
    match x with
    | AddMount(Mount.MountPoint target, Mount.BdevPath source,
               Mount.FsType fstype, Mount.MountOpts opts) ->
        Set.add ({ target = target
                   source = source
                   fstype = fstype
                   opts = opts }) localMounts
    | RemoveMount(Mount.MountPoint target, Mount.BdevPath source,
                  Mount.FsType fstype, Mount.MountOpts opts) ->
        Set.remove ({ target = target
                      source = source
                      fstype = fstype
                      opts = opts }) localMounts
    | ReplaceMount(Mount.MountPoint target, Mount.BdevPath source,
                   Mount.FsType fstype, Mount.MountOpts opts,
                   Mount.MountOpts oldOpts) ->
        Set.remove ({ target = target
                      source = source
                      fstype = fstype
                      opts = oldOpts }) localMounts
        |> Set.add ({ target = target
                      source = source
                      fstype = fstype
                      opts = opts })
    | MoveMount(Mount.MountPoint target, Mount.BdevPath source,
                Mount.FsType fstype, Mount.MountOpts opts,
                Mount.MountPoint oldTarget) ->
        Set.remove ({ target = oldTarget
                      source = source
                      fstype = fstype
                      opts = opts }) localMounts
        |> Set.add ({ target = target
                      source = source
                      fstype = fstype
                      opts = opts })
    |> Ok
