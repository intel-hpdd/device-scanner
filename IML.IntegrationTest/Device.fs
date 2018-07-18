// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.
[<RequireQualifiedAccess>]
module IML.IntegrationTest.Device

open Fable.Import
open Fable.Import.Node.PowerPack
open IML.IntegrationTestFramework.IntegrationTestFramework

let rbScanForDisk() : RollbackState -> RollbackCommandState =
    rbCmd 
        "for host in `ls /sys/class/scsi_host`; do echo \"- - -\" > /sys/class/scsi_host/$host/scan; done"
let rbSetDeviceState (name : string) (state : string) : RollbackState -> RollbackCommandState =
    rbCmd (sprintf "echo \"%s\" > /sys/block/%s/device/state" state name)
let setDeviceState (name : string) (state : string) : State -> JS.Promise<CommandResult<Out, Err>> =
    cmd (sprintf "echo \"%s\" > /sys/block/%s/device/state" state name)
let deleteDevice (name : string) : State -> JS.Promise<CommandResult<Out, Err>> =
    cmd (sprintf "echo \"1\" > /sys/block/%s/device/delete" name)
let scanForDisk() =
    cmd 
        "for host in `ls /sys/class/scsi_host`; do echo \"- - -\" > /sys/class/scsi_host/$host/scan; done"
