// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.
module IML.IntegrationTest.IntegrationTest

open Fable.PowerPack
open Thoth.Json
open Fable.Import
open Fable.Core.JsInterop
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open IML.CommonLibrary
open IML.Types.UeventTypes
open IML.StatefulPromise.StatefulPromise
open IML.IntegrationTestFramework.IntegrationTestFramework
open Fable.Import.Jest
open Matchers

type PartitionFlag = Raid

let env = Globals.``process``.env
let testInterface1 = !!env?TEST_INTERFACE_1
let testInterface2 = !!env?TEST_INTERFACE_2
let testInterface3 = !!env?TEST_INTERFACE_3
let settle() = cmd "udevadm settle" >> ignoreCmd
let rbSettle() = rbCmd "udevadm settle"
let sleep seconds = cmd (sprintf "sleep %d" seconds)
let scannerInfo =
    (fun _ ->
    pipeToShellCmd "echo '\"Stream\"'"
        "socat - UNIX-CONNECT:/var/run/device-scanner.sock") >>= settle()
let rbScanForDisk() : RollbackState -> RollbackCommandState =
    rbCmd
        "for host in `ls /sys/class/scsi_host`; do echo \"- - -\" > /sys/class/scsi_host/$host/scan; done"
let rbSetDeviceState (name : string) (state : string) : RollbackState -> RollbackCommandState =
    rbCmd (sprintf "echo \"%s\" > /sys/block/%s/device/state" state name)
let rbRmPart (device : string) (partId : int) =
    rbCmd (sprintf "parted %s -s rm %d" device partId)
let rbWipefs (device : string) = rbCmd (sprintf "wipefs -a %s" device)
let setDeviceState (name : string) (state : string) : State -> JS.Promise<CommandResult<Out, Err>> =
    cmd (sprintf "echo \"%s\" > /sys/block/%s/device/state" state name)
let deleteDevice (name : string) : State -> JS.Promise<CommandResult<Out, Err>> =
    cmd (sprintf "echo \"1\" > /sys/block/%s/device/delete" name)
let scanForDisk() =
    cmd
        "for host in `ls /sys/class/scsi_host`; do echo \"- - -\" > /sys/class/scsi_host/$host/scan; done"

let resultOutput : StatefulResult<State, Out, Err> -> string =
    function
    | Ok((Stdout(r), _), _) -> r
    | Error(e) -> failwithf "Command failed: %A" e

let mkLabel (disk : string) (label : string) =
    cmd (sprintf "parted %s -s mklabel %s" disk label)
let mkPart (disk : string) (diskType : string) (start : int) (finish : int) =
    cmd
        (sprintf "parted -a opt %s -s mkpart %s ext4 %d %d" disk diskType start
             finish)
let mkfs (fstype : string) (disk : string) =
    cmd (sprintf "mkfs -t %s %s" fstype disk)
let e2Label (disk : string) (label : string) =
    cmd (sprintf "e2label %s %s" disk label)

let setPartitionFlag (disk : string) (partitionId : int)
    (partitionFlag : PartitionFlag) =
    let cmdString =
        match partitionFlag with
        | PartitionFlag.Raid ->
            sprintf "parted %s set %d raid on" disk partitionId
    cmd cmdString

let serializeDecodedAndMatch (r, _) =
    r
    |> resultOutput
    |> Decode.decodeString (Decode.field "blockDevices" BlockDevices.decoder)
    |> Result.unwrap
    |> UdevSerializer.serialize
    |> BlockDevices.encoder
    |> Encode.encode 2
    |> toMatchSnapshot

let iscsiDiscoverIF1 = ISCSIAdm.iscsiDiscover testInterface1
let iscsiLoginIF1 = ISCSIAdm.iscsiLogin testInterface1
let iscsiLogoutIF1 = ISCSIAdm.iscsiLogout testInterface1
let iscsiDiscoverIF2 = ISCSIAdm.iscsiDiscover testInterface2
let iscsiLoginIF2 = ISCSIAdm.iscsiLogin testInterface2
let iscsiLogoutIF2 = ISCSIAdm.iscsiLogout testInterface2
let iscsiDiscoverIF3 = ISCSIAdm.iscsiDiscover testInterface3
let iscsiLoginIF3 = ISCSIAdm.iscsiLogin testInterface3
let iscsiLogoutIF3 = ISCSIAdm.iscsiLogout testInterface3

testAsync "stream event" <| fun () ->
    command { return! scannerInfo }
    |> startCommand "Stream Event"
    |> Promise.map serializeDecodedAndMatch
testAsync "remove a device" <| fun () ->
    command {
        do! (setDeviceState "sdc" "offline")
            >> rollbackError (rbSetDeviceState "sdc" "running")
            >> ignoreCmd
        do! (deleteDevice "sdc")
            >> rollback (rbScanForDisk())
            >> ignoreCmd
        return! scannerInfo
    }
    |> startCommand "removing a device"
    |> Promise.map serializeDecodedAndMatch
testAsync "add a device" <| fun () ->
    command {
        do! (setDeviceState "sdc" "offline")
            >> rollbackError (rbSetDeviceState "sdc" "running")
            >> ignoreCmd
        do! (deleteDevice "sdc")
            >> rollbackError (rbScanForDisk())
            >> ignoreCmd
        do! (scanForDisk()) >> ignoreCmd
        return! scannerInfo
    }
    |> startCommand "adding a device"
    |> Promise.map serializeDecodedAndMatch
testAsync "create a partition" <| fun () ->
    command {
        do! (mkLabel "/dev/sdc" "gpt") >> ignoreCmd
        do! (mkPart "/dev/sdc" "primary" 1 100)
            >> rollback (rbRmPart "/dev/sdc" 1)
            >> ignoreCmd
        do! (sleep 1) >> ignoreCmd
        do! (mkfs "ext4" "/dev/sdc1")
            >> rollback (rbWipefs "/dev/sdc1")
            >> ignoreCmd
        do! (e2Label "/dev/sdc1" "black_label") >> ignoreCmd
        return! scannerInfo
    }
    |> startCommand "creating a partition"
    |> Promise.map serializeDecodedAndMatch
testAsync "add multipath device" <| fun () ->
    command {
        do! cmd (iscsiDiscoverIF1()) >> ignoreCmd
        do! cmd (iscsiLoginIF1())
            >> rollback (rbCmd ("sleep 1"))
            >> rollback (rbSettle())
            >> rollback (rbCmd (iscsiLogoutIF1()))
            >> ignoreCmd
        do! cmd (iscsiDiscoverIF2()) >> ignoreCmd
        do! cmd (iscsiLoginIF2())
            >> rollback (rbCmd (iscsiLogoutIF2()))
            >> ignoreCmd
        do! cmd (iscsiDiscoverIF3()) >> ignoreCmd
        do! cmd (iscsiLoginIF3())
            >> rollback (rbCmd (iscsiLogoutIF3()))
            >> ignoreCmd
        return! scannerInfo
    }
    |> startCommand "add multipath device"
    |> Promise.map serializeDecodedAndMatch

testAsync "add mdraid" <| fun () ->
    let createMd0 = MdRaid.createMdRaid "/dev/md0"
    let stopMd0 = MdRaid.stopMdRaid "/dev/md0"
    let cleanSdd1 = MdRaid.cleanPartition "/dev/sdd1"
    let cleanSde1 = MdRaid.cleanPartition "/dev/sde1"
    let cleanMdParts =
        rollback (rbCmd (cleanSde1())) >> rollback (rbCmd (cleanSdd1()))
    command {
        do! (mkLabel "/dev/sdd" "gpt")
            >> rollback (rbWipefs "/dev/sdd")
            >> ignoreCmd
        do! (mkLabel "/dev/sde" "gpt")
            >> rollback (rbWipefs "/dev/sde")
            >> ignoreCmd
        do! (mkPart "/dev/sdd" "primary" 1 100)
            >> rollback (rbRmPart "/dev/sdd" 1)
            >> ignoreCmd
        do! (mkPart "/dev/sde" "primary" 1 100)
            >> rollback (rbRmPart "/dev/sde" 1)
            >> ignoreCmd
        do! (setPartitionFlag "/dev/sdd" 1 PartitionFlag.Raid) >> ignoreCmd
        do! (setPartitionFlag "/dev/sde" 1 PartitionFlag.Raid) >> ignoreCmd
        do! settle()
        do! cmd (createMd0 "/dev/sd[d-e]" ())
            >> cleanMdParts
            >> rollback (rbCmd (stopMd0()))
            >> ignoreCmd
        do! settle()
        do! (mkfs "ext4" "/dev/md0")
            >> rollbackError (rbWipefs "/dev/md0")
            >> ignoreCmd
        return! scannerInfo
    }
    |> startCommand "add mdraid"
    |> Promise.map serializeDecodedAndMatch
