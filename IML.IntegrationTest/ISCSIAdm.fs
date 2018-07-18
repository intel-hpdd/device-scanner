// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

[<RequireQualifiedAccess>]
module IML.IntegrationTest.ISCSIAdm

type Mode = Discovery | Node | Mfw | Host | Iface | Session
type Type = SendTargets | Slp | Isns | Tfw


let defaultPort = 3260
let defaultTargetName = "iqn.2018-03.com.test:server"

let iscsiAdm () =
  "iscsiadm"

module Mode =
  let mode (mode:Mode) (x:string) =
    match mode with
      | Mode.Discovery -> sprintf("%s -m discovery") x
      | Mode.Node -> sprintf("%s -m node") x
      | Mode.Mfw -> sprintf("%s -m fw") x
      | Mode.Host -> sprintf("%s -m host") x
      | Mode.Iface -> sprintf("%s -m iface") x
      | Mode.Session -> sprintf("%s -m session") x

module Type =
  let ``type`` (t:Type) (x:string) =
    match t with
      | Type.SendTargets -> sprintf("%s -t st") x
      | Type.Slp -> sprintf("%s -t slp") x
      | Type.Isns -> sprintf("%s -t isns") x
      | Type.Tfw -> sprintf("%s -t fw") x

let private portal (ip:string) (port:int) (x:string) =
  sprintf("%s -p %s:%d") x ip port

let private targetName (target:string) (x:string) =
  sprintf("%s -T %s") x target

let private login (x:string) =
  sprintf("%s -l") x

let private logout (x:string) =
  sprintf("%s -u") x

let iscsiDiscover (ip:string) =
  iscsiAdm
    >> (Mode.mode Mode.Discovery)
    >> (Type.``type`` Type.SendTargets)
    >> (portal ip defaultPort)

let iscsiConnection (ip:string) =
  iscsiAdm
    >> (Mode.mode Mode.Node)
    >> (targetName defaultTargetName)
    >> (portal ip defaultPort)

let iscsiLogin (ip:string) = (iscsiConnection ip) >> login
let iscsiLogout (ip:string) = (iscsiConnection ip) >> logout
