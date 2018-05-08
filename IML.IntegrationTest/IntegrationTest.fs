// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.
module IML.IntegrationTest.IntegrationTest

open Fable.PowerPack
open Thoth.Json
open Fable.Core.JsInterop
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open IML.CommonLibrary
open IML.Types.UeventTypes
open IML.StatefulPromise.StatefulPromise
open IML.IntegrationTestFramework.IntegrationTestFramework
open Fable.Import.Jest
open Matchers

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
            >> rollback (Filesystem.rbWipefs "/dev/sdc1")
            >> ignoreCmd
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
        do! (Parted.setPartitionFlag "/dev/sdd" 1 Parted.PartitionFlag.Raid) >> ignoreCmd
        do! (Parted.setPartitionFlag "/dev/sde" 1 Parted.PartitionFlag.Raid) >> ignoreCmd
        do! settle()
        do! MdRaid.MdRaidCommand.createRaidAndRollback "/dev/sd[d-e]" "/dev/md0"
                [ "/dev/sdd1"; "/dev/sde1" ]
        do! settle()
        do! MdRaid.MdRaidCommand.createRaidFs "ext4" "/dev/md0"
        return! scannerInfo
    }
    |> startCommand "add mdraid"
    |> Promise.map serializeDecodedAndMatch
