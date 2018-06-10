// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTest.SerializerCommon

open IML.Types.UeventTypes
open System.Text.RegularExpressions

let k p _ =
  p

let replace (regexp:string) (replacement:string) (path:string) =
  Regex.Replace (path, regexp, replacement)

let normalizeByPath (regex:string) (replacement:string) (Path(path)) =
  Regex.Replace (
    path,
    regex,
    sprintf "$1%s" replacement
  )
    |> Path

let normalizeByUUIDPath (path:Path) =
  normalizeByPath "(/dev/disk/by-uuid/).+" "uuid-XXXXX" path

let normalizeByLVMUUID (path:Path) =
  normalizeByPath "(/dev/disk/by-id/)lvm-pv-uuid-.+" "lvm-pv-uuid-XXXXX" path

let normalizeByDmUUUID (path:Path) =
  normalizeByPath "(/dev/disk/by-id/)dm-uuid-LVM-.+" "dm-uuid-LVM-XXXXX" path

let normalizeByPartUUID (path:Path) =
  normalizeByPath "(/dev/disk/by-partuuid/).+" "part-uuid-XXXXX" path

let normalizeByIp (Path(path)) =
  Regex.Replace (
    path,
    "/dev/disk/by-path/ip-.+-iscsi-iqn.2018-03.com.test:server-lun-([0-9]+)",
    "/dev/disk/by-path/ip-172.28.128.X:3260-iscsi-iqn.2018-03.com.test:server-lun-$1"
  )
    |> Path

let normalizeByMdUUID (path:Path) =
  normalizeByPath "(/dev/disk/by-id/)md-uuid-.+" "md-uuid-aa:bb:cc:dd" path

let normalizeByMdName (path:Path) =
  normalizeByPath "(/dev/disk/by-id/)md-name-.+" "md-name-mdname" path
