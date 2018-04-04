// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

[<RequireQualifiedAccess>]
module IML.IntegrationTest.ISCSIAdm

open Fable.Import
open Fable.Import.Jest
open Matchers
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open Fable.PowerPack
open Json
open System.Xml.Linq

type MODE = DISCOVERY | NODE | M_FW | HOST | IFACE | SESSION
type TYPE = SEND_TARGETS | SLP | ISNS | T_FW


[<Literal>]
let defaultPort = 3260
[<Literal>]
let defaultTargetName = "iqn.2018-03.com.test:server"

let iscsiAdm () =
  "iscsiadm"

let private mode (mode:MODE) (x:string) =
  match mode with
    | DISCOVERY _ -> sprintf("%s -m discovery") x
    | NODE _ -> sprintf("%s -m node") x
    | M_FW _ -> sprintf("%s -m fw") x
    | HOST _ -> sprintf("%s -m host") x
    | IFACE _ -> sprintf("%s -m iface") x
    | SESSION _ -> sprintf("%s -m session") x


let private ``type`` (t:TYPE) (x:string) =
  match t with
    | SEND_TARGETS _ -> sprintf("%s -t st") x
    | SLP _ -> sprintf("%s -t slp") x
    | ISNS _ -> sprintf("%s -t isns") x
    | T_FW _ -> sprintf("%s -t fw") x

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
    >> (mode DISCOVERY)
    >> (``type`` SEND_TARGETS)
    >> (portal ip defaultPort)

let iscsiConnection (ip:string) =
  iscsiAdm
    >> (mode NODE)
    >> (targetName defaultTargetName)
    >> (portal ip defaultPort)

let iscsiLogin (ip:string) = (iscsiConnection ip) >> login
let iscsiLogout (ip:string) = (iscsiConnection ip) >> logout
