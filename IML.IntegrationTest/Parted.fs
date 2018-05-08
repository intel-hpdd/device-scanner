// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.
[<RequireQualifiedAccess>]
module IML.IntegrationTest.Parted

open IML.IntegrationTestFramework.IntegrationTestFramework

type PartitionFlag = Raid

let rbRmPart (device : string) (partId : int) =
    rbCmd (sprintf "parted %s -s rm %d" device partId)
let mkLabel (disk : string) (label : string) =
    cmd (sprintf "parted %s -s mklabel %s" disk label)
let mkPart (disk : string) (diskType : string) (start : int) (finish : int) =
    cmd
        (sprintf "parted -a opt %s -s mkpart %s ext4 %d %d" disk diskType start
             finish)

let setPartitionFlag (disk : string) (partitionId : int)
    (partitionFlag : PartitionFlag) =
    let cmdString =
        match partitionFlag with
        | PartitionFlag.Raid ->
            sprintf "parted %s set %d raid on" disk partitionId
    cmd cmdString >> ignoreCmd

let mkLabelAndRollback (device : string) (partType : string) =
    (mkLabel device partType)
    >> rollback (Filesystem.rbWipefs device)
    >> ignoreCmd

let mkPartAndRollback (device : string) (partType : string) (start : int)
    (finish : int) =
    (mkPart device partType start finish)
    >> rollback (rbRmPart device 1)
    >> ignoreCmd
