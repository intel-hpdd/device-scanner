// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.
[<RequireQualifiedAccess>]
module IML.IntegrationTest.LVM

open IML.IntegrationTestFramework.IntegrationTestFramework

let private concat : string List -> string = String.concat " "
let private addArg (arg : string) (x : string) = sprintf "%s %s" x arg

module PhysicalVolume =
    let pvCreate (blockDevices : string List) =
        let devicesString = blockDevices |> concat
        sprintf "pvcreate %s" devicesString
    
    let pvRemove (blockDevices : string List) =
        let devicesString = blockDevices |> concat
        sprintf "pvremove %s" devicesString

module VolumeGroup =
    let vgCreate (name : string) (blockDevices : string List) =
        let devicesString = blockDevices |> concat
        sprintf "vgcreate %s %s" name devicesString
    
    let vgChange (name : string) =
        function 
        | true -> sprintf "vgchange -a y %s" name
        | false -> sprintf "vgchange -a n %s" name
    
    let vgRemove (name : string) = sprintf "vgremove %s" name

module LogicalVolume =
    let lvmChange (name : string) =
        function 
        | true -> sprintf "lvchange -a y %s" name
        | false -> sprintf "lvchange -a n %s" name
    
    let private lvmCreate() = "lvcreate"
    let private lvmSize (L : string) (x : string) = sprintf "%s -L %s" x L
    let private lvmName (n : string) (x : string) = sprintf "%s -n %s" x n
    let private lvmStripe (pvsToSpan : int) (stripeUnit : int) (x : string) =
        sprintf "%s -I %d -i %d" x pvsToSpan stripeUnit
    let lvmRemove (path : string) = sprintf "lvremove %s" path
    
    let createStriped (size : string) (pvs : int) (stripeUnit : int) 
        (name : string) (groupName : string) =
        lvmCreate
        >> lvmSize size
        >> lvmStripe pvs stripeUnit
        >> lvmName name
        >> addArg groupName
