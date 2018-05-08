// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.
[<RequireQualifiedAccess>]
module IML.IntegrationTest.MdRaid

open IML.IntegrationTestFramework.IntegrationTestFramework

type private Mode =
    | Create
    | Manage
    | Misc

module private Mode =
    let operation (op : Mode) (mdDevice : string) (x : string) =
        match op with
        | Mode.Create -> sprintf "%s --create %s --force" x mdDevice
        | Mode.Manage -> sprintf "%s --manage %s" x mdDevice
        | Mode.Misc -> sprintf "%s --misc" x

type private Level =
    | Linear
    | Raid0
    | Zero
    | Stripe
    | Raid1
    | One
    | Mirror
    | Raid4
    | Four
    | Raid5
    | Five
    | Raid6
    | Six
    | Raid10
    | Ten
    | Multipath
    | Mp
    | Faulty
    | Container

type private Create =
    | Level
    | RaidDevices

module private Create =
    let private levelOption = sprintf "%s --level=%s"
    
    let level (lvl : Level) (x : string) =
        match lvl with
        | Level.Linear -> levelOption x "linear"
        | Level.Raid0 -> levelOption x "raid0"
        | Level.Zero -> levelOption x "0"
        | Level.Stripe -> levelOption x "stripe"
        | Level.Raid1 -> levelOption x "raid1"
        | Level.One -> levelOption x "1"
        | Level.Mirror -> levelOption x "mirror"
        | Level.Raid4 -> levelOption x "raid4"
        | Level.Four -> levelOption x "4"
        | Level.Raid5 -> levelOption x "raid5"
        | Level.Five -> levelOption x "5"
        | Level.Raid6 -> levelOption x "raid6"
        | Level.Six -> levelOption x "6"
        | Level.Raid10 -> levelOption x "raid10"
        | Level.Ten -> levelOption x "10"
        | Level.Multipath -> levelOption x "multipath"
        | Level.Mp -> levelOption x "mp"
        | Level.Faulty -> levelOption x "faulty"
        | Level.Container -> levelOption x "container"
    
    let raidDevices (numDevices : int) (x : string) =
        sprintf "%s --raid-devices=%d" x numDevices

module private Manage =
    let stop (x : string) = sprintf "%s --stop" x

module private Misc =
    let zeroSuperblock (partPath : string) (x : string) =
        sprintf "%s --zero-superblock %s" x partPath

module private MdRaidOperation =
    let private mdAdm() = "mdadm"
    let private addArg (arg : string) (x : string) = sprintf "%s %s" x arg
    let private yesPipe (x : string) = sprintf "yes | %s" x
    
    let createMdRaid (mdDeviceName : string) (devices : string) =
        mdAdm
        >> (Mode.operation Mode.Create mdDeviceName)
        >> (Create.level Level.Mirror)
        >> (Create.raidDevices 2)
        >> (addArg devices)
        >> yesPipe
    
    let cleanPartition (partPath : string) =
        mdAdm
        >> (Mode.operation Mode.Misc "")
        >> (Misc.zeroSuperblock partPath)
    
    let stopMdRaid (mdDeviceName : string) =
        mdAdm
        >> (Mode.operation Mode.Manage mdDeviceName)
        >> (Manage.stop)

module MdRaidCommand =
    let private cleanPartitions (deviceParts : string List) =
        let folder state curDevice =
            let fn =
                rollback (rbCmd (MdRaidOperation.cleanPartition curDevice ()))
            state >> fn
        List.fold folder id deviceParts
    
    let createRaidAndRollback (devices : string) (raidPath : string) 
        (raidDeviceParts : string List) =
        cmd (MdRaidOperation.createMdRaid raidPath devices ())
        >> cleanPartitions raidDeviceParts
        >> rollback (rbCmd (MdRaidOperation.stopMdRaid raidPath ()))
        >> ignoreCmd
