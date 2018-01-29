// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.HistoryEventZedlet

open IML.Types.CommandTypes
open IML.Listeners.CommonLibrary

let historyName = Zed.getHistoryName()
let guid:Guid = Zed.getGuid()

let zfsNameMaybe = Zed.getZfsNameMaybe()

let cmd = 
  if historyName = Zed.Create && Option.isSome zfsNameMaybe then
    Some (CreateZfs(guid, Zed.getZfsName()))
  else if historyName = Zed.Destroy then
    Some (DestroyZfs(guid, Zed.getZfsName()))
  else if historyName = Zed.Set then
    let (key, value) =
      Zed.getHistoryStr()
        |> fun (x:string) -> x.Split '='
        |> fun xs -> (Array.head xs, Array.last xs)

    match zfsNameMaybe with
      | Some(x) -> 
        Some(SetZfsProp(guid, x, key, value))
      | None ->
        Some(SetZpoolProp(guid, key, value))
  else
    None

match cmd with
  | Some cmd' ->
    sendData (ZedCommand cmd')
  | None  -> ()
