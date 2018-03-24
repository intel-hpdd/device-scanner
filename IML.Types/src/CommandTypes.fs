// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.CommandTypes

open Fable.Core

[<Erase>]
type BdevPath = BdevPath of string
[<Erase>]
type MountPoint = MountPoint of string
[<Erase>]
type FsType = FsType of string

[<RequireQualifiedAccess>]
module Zpool =
  [<Erase>]
  type Guid = Guid of string

  [<Erase>]
  type Name = Name of string

  [<Erase>]
  type State = State of string

[<RequireQualifiedAccess>]
module Zfs =
  [<Erase>]
  type Name = Name of string

[<RequireQualifiedAccess>]
module Prop =
  type Key = string
  type Value = string

[<RequireQualifiedAccess>]
module Vdev =
  [<Erase>]
  type Guid = Guid of string

  [<Erase>]
  type State = State of string

type ZedCommand =
  | Init
  | CreateZpool of Zpool.Name * Zpool.Guid * Zpool.State
  | ImportZpool of Zpool.Guid * Zpool.State
  | ExportZpool of Zpool.Guid * Zpool.State
  | DestroyZpool of Zpool.Guid
  | CreateZfs of Zpool.Guid * Zfs.Name
  | DestroyZfs of Zpool.Guid * Zfs.Name
  | SetZpoolProp of Zpool.Guid * Prop.Key * Prop.Value
  | SetZfsProp of Zpool.Guid * Zfs.Name * Prop.Key * Prop.Value
  | AddVdev of Zpool.Guid

type UdevCommand =
  | Add of string
  | Change of string
  | Remove of string

[<RequireQualifiedAccess>]
module Mount =
  [<Erase>]
  type MountPoint = MountPoint of string
  [<Erase>]
  type BdevPath = BdevPath of string
  [<Erase>]
  type FsType = FsType of string
  [<Erase>]
  type Options = Options of string

type MountCommand =
  | Mount of Mount.MountPoint * Mount.BdevPath * Mount.FsType * Mount.Options
  | Umount of Mount.MountPoint
  | Remount of Mount.MountPoint * Mount.BdevPath * Mount.FsType * Mount.Options
  | Move of Mount.MountPoint * Mount.BdevPath * Mount.FsType * Mount.Options

/// This is for backcompat with v1
/// of device-scanner.
/// Once we stop supporting v1 of device-scanner, we
/// can drop this.
[<StringEnum>]
type ACTION =
  | Info

type Command =
  | Info
  | ACTION of ACTION
  | ZedCommand of ZedCommand
  | UdevCommand of UdevCommand
  | MountCommand of MountCommand
