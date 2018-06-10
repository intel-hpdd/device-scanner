// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.
module IML.IntegrationTest.IntegrationTest

open Fable.PowerPack
open Thoth.Json
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open IML.CommonLibrary
open IML.Types.UeventTypes
open IML.StatefulPromise.StatefulPromise
open IML.IntegrationTestFramework.IntegrationTestFramework
open Fable.Import.Jest
open Matchers
open IML.Types.LegacyTypes

let env = Globals.``process``.env
let testInterface1 = "10.0.0.30"
let testInterface2 = "10.0.0.40"
let testInterface3 = "10.0.0.50"
let settle() = cmd "udevadm settle" >> ignoreCmd
let rbSettle() = rbCmd "udevadm settle"
let sleep seconds = cmd (sprintf "sleep %d" seconds)
let scannerInfo =
    (fun _ ->
    pipeToShellCmd "echo '\"Stream\"'"
        "socat - UNIX-CONNECT:/var/run/device-scanner.sock") >>= settle()

let resultOutput : StatefulResult<State, Out, Err> -> string =
    function
    | Ok((Stdout(r), _), _) -> r
    | Error(e) -> failwithf "Command failed: %A" e

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
        do! (Device.setDeviceState "sdc" "offline")
            >> rollbackError (Device.rbSetDeviceState "sdc" "running")
            >> ignoreCmd
        do! (Device.deleteDevice "sdc")
            >> rollback (Device.rbScanForDisk())
            >> ignoreCmd
        return! scannerInfo
    }
    |> startCommand "removing a device"
    |> Promise.map serializeDecodedAndMatch
testAsync "add a device" <| fun () ->
    command {
        do! (Device.setDeviceState "sdc" "offline")
            >> rollbackError (Device.rbSetDeviceState "sdc" "running")
            >> ignoreCmd
        do! (Device.deleteDevice "sdc")
            >> rollbackError (Device.rbScanForDisk())
            >> ignoreCmd
        do! (Device.scanForDisk()) >> ignoreCmd
        return! scannerInfo
    }
    |> startCommand "adding a device"
    |> Promise.map serializeDecodedAndMatch
testAsync "create a partition" <| fun () ->
    command {
        do! (Parted.mkLabel "/dev/sdc" "gpt") >> ignoreCmd
        do! (Parted.mkPart "/dev/sdc" "primary" 1 100)
            >> rollback (Parted.rbRmPart "/dev/sdc" 1)
            >> ignoreCmd
        do! (sleep 1) >> ignoreCmd
        do! (Filesystem.mkfs "ext4" "/dev/sdc1")
        do! (Filesystem.e2Label "/dev/sdc1" "black_label") >> ignoreCmd
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
    command {
        do! Parted.mkLabelAndRollback "/dev/sdd" "gpt"
        do! Parted.mkLabelAndRollback "/dev/sde" "gpt"
        do! Parted.mkPartAndRollback "/dev/sdd" "primary" 1 100
        do! Parted.mkPartAndRollback "/dev/sde" "primary" 1 100
        do! (Parted.setPartitionFlag "/dev/sdd" 1 Parted.PartitionFlag.Raid)
        do! (Parted.setPartitionFlag "/dev/sde" 1 Parted.PartitionFlag.Raid)
        do! settle()
        do! MdRaid.MdRaidCommand.createRaidAndRollback "/dev/sd[d-e]1"
                "/dev/md0" [ "/dev/sdd1"; "/dev/sde1" ]
        do! settle()
        do! Filesystem.mkfs "ext4" "/dev/md0"
        do! settle()
        return! scannerInfo
    }
    |> startCommand "add mdraid"
    |> Promise.map serializeDecodedAndMatch
testAsync "add logical volume" <| fun () ->
    command {
        do! LVM.LVMCommand.createPhysicalVolumesAndRollback
                [ "/dev/sdd"; "/dev/sde"; "/dev/sdf" ]
        do! LVM.LVMCommand.createVolumeGroupAndRollback "vg01"
                [ "/dev/sdd"; "/dev/sde"; "/dev/sdf" ]
        do! LVM.LVMCommand.activateVolumeGroup "vg01"
        do! LVM.LVMCommand.createStripedVolume "200m" 4096 3 "lvol01" "vg01"
                "/dev/vg01/lvol01"
        do! Filesystem.mkfs "ext4" "/dev/vg01/lvol01"
        return! scannerInfo
    }
    |> startCommand "add logical volume"
    |> Promise.map serializeDecodedAndMatch
testAsync "verify device data from aggregator daemon" <| fun () ->
    command {
        return! scanDeviceAggregator
    }
    |> startCommand "verify device data from aggregator daemon"
    |> Promise.map (fun (r, _) ->
          r
            |> resultOutput
            |> Decode.decodeString (Decode.field "10.0.0.20" LegacyDevTree.decode)
            |> Result.unwrap
            |> DevTreeSerializer.serialize
            |> LegacyDevTree.encode
            |> Encode.encode 2
            |> toMatchSnapshot
    )
