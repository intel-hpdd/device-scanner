// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTest.IntegrationTest

open Fable.PowerPack
open Thot
open Fable.Import
open Fable.Import.Node.PowerPack
open IML.CommonLibrary
open IML.Types.UeventTypes
open IML.StatefulPromise.StatefulPromise
open IML.IntegrationTestFramework.IntegrationTestFramework
open Fable.Import.Jest
open Matchers

let settle () =
  cmd "udevadm settle"
    >> ignoreCmd

let sleep seconds =
  cmd (sprintf "sleep %d" seconds)

let scannerInfo =
  (fun _ -> pipeToShellCmd "echo '\"Stream\"'" "socat - UNIX-CONNECT:/var/run/device-scanner.sock")
    >>= settle()

let rbScanForDisk (): RollbackState -> RollbackCommandState =
  rbCmd "for host in `ls /sys/class/scsi_host`; do echo \"- - -\" > /sys/class/scsi_host/$host/scan; done"

let rbSetDeviceState (name:string) (state:string): RollbackState -> RollbackCommandState =
  rbCmd (sprintf "echo \"%s\" > /sys/block/%s/device/state" state name)

let rbRmPart (device:string) (partId:int) =
  rbCmd (sprintf "parted %s -s rm %d" device partId)

let rbWipefs (device:string) =
  rbCmd (sprintf "wipefs -a %s" device)

let setDeviceState (name:string) (state:string): State -> JS.Promise<CommandResult<Out, Err>> =
  cmd (sprintf "echo \"%s\" > /sys/block/%s/device/state" state name)

let deleteDevice (name:string): State -> JS.Promise<CommandResult<Out, Err>> =
  cmd (sprintf "echo \"1\" > /sys/block/%s/device/delete" name)

let scanForDisk () =
  cmd "for host in `ls /sys/class/scsi_host`; do echo \"- - -\" > /sys/class/scsi_host/$host/scan; done"

let resultOutput: StatefulResult<State, Out, Err> -> string = function
  | Ok ((Stdout(r), _), _) -> r
  | Error (e) -> failwithf "Command failed: %A" e

let mkLabel (disk:string) (label:string) =
  cmd (sprintf "parted %s -s mklabel %s" disk label)

let mkPart (disk:string) (diskType:string) (start:int) (finish:int) =
  cmd (sprintf "parted -a opt %s -s mkpart %s ext4 %d %d" disk diskType start finish)

let mkfs (fstype:string) (disk:string) =
  cmd (sprintf "mkfs -t %s %s" fstype disk)

let e2Label  (disk:string) (label:string) =
  cmd (sprintf "e2label %s %s" disk label)

let serializeDecodedAndMatch (r, _) =
  r
    |> resultOutput
    |> Json.Decode.decodeString (Thot.Json.Decode.field "blockDevices" BlockDevices.decoder)
    |> Result.unwrap
    |> UdevSerializer.serialize
    |> BlockDevices.encoder
    |> Json.Encode.encode 2
    |> toMatchSnapshot

testAsync "stream event" <| fun () ->
  command {
    return! scannerInfo
  }
  |> startCommand "Stream Event"
  |> Promise.map serializeDecodedAndMatch

testAsync "remove a device" <| fun () ->
  command {
    do! (setDeviceState "sdc" "offline") >> rollbackError (rbSetDeviceState "sdc" "running") >> ignoreCmd
    do! (deleteDevice "sdc") >> rollback (rbScanForDisk ()) >> ignoreCmd
    return! scannerInfo
  }
  |> startCommand "removing a device"
  |> Promise.map serializeDecodedAndMatch

testAsync "add a device" <| fun () ->
  command {
    do! (setDeviceState "sdc" "offline") >> rollbackError (rbSetDeviceState "sdc" "running") >> ignoreCmd
    do! (deleteDevice "sdc") >> rollbackError (rbScanForDisk ()) >> ignoreCmd
    do! (scanForDisk ()) >> ignoreCmd
    return! scannerInfo
  }
  |> startCommand "adding a device"
  |> Promise.map serializeDecodedAndMatch

testAsync "create a partition" <| fun () ->
  command {
    do! (mkLabel "/dev/sdc" "gpt") >> ignoreCmd
    do! (mkPart "/dev/sdc" "primary" 1 100) >> rollback (rbRmPart "/dev/sdc" 1) >> ignoreCmd
    do! (sleep 1) >> ignoreCmd
    do! (mkfs "ext4" "/dev/sdc1") >> rollback (rbWipefs "/dev/sdc1") >> ignoreCmd
    do! (e2Label "/dev/sdc1" "black_label") >> ignoreCmd
    return! scannerInfo
  }
  |> startCommand "creating a partition"
  |> Promise.map serializeDecodedAndMatch
