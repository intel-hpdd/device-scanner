// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.
[<RequireQualifiedAccess>]
module IML.IntegrationTest.Filesystem

open IML.IntegrationTestFramework.IntegrationTestFramework

let rbWipefs (device : string) = rbCmd (sprintf "wipefs -a %s" device)

let mkfs (fstype : string) (disk : string) =
    cmd (sprintf "mkfs -t %s %s" fstype disk)
    >> rollback (rbWipefs disk)
    >> ignoreCmd

let e2Label (disk : string) (label : string) =
    cmd (sprintf "e2label %s %s" disk label)

let wipeFilesystems (blockDevices : string List) =
    let folder state curDevice =
        let fn = rollback (rbWipefs curDevice)
        state >> fn
    List.fold folder id blockDevices
