// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.Types.CommandTypes

open Fable.Core

[<Erase>]
type Guid = Guid of string
[<Erase>]
type ZpoolName = ZpoolName of string 
[<Erase>]
type ZfsName = ZfsName of string
[<Erase>]
type State = State of string 

type Key = string 
type Value = string 

type ZedCommand = 
  | Init 
  | CreateZpool of ZpoolName * Guid * State
  | ImportZpool of Guid * State
  | ExportZpool of Guid * State
  | DestroyZpool of Guid
  | CreateZfs of Guid * ZfsName
  | DestroyZfs of Guid * ZfsName
  | SetZpoolProp of Guid * Key * Value
  | SetZfsProp of Guid * ZfsName * Key * Value 
  | AddVdev of Guid

type UdevCommand = 
  | Add of string
  | Change of string
  | Remove of string

type Command =
  | Info
  | ZedCommand of ZedCommand 
  | UdevCommand of UdevCommand