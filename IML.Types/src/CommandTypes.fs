// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.CommandTypes

open Fable.Core

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

/// This is for backcompat with v1
/// of device-scanner.
/// Once we stop supporting v1 of device-scanner, we
/// can drop this.
[<StringEnum>]
type ACTION =
  | Info

type Command =
  | Info
  | ZedCommand of ZedCommand
  | UdevCommand of UdevCommand
  | ACTION of ACTION
