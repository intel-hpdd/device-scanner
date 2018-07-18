// Copyright (c) 2018 DDN. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.HistoryEventZedlet

open IML.Types.CommandTypes
open IML.Listeners.CommonLibrary

let historyName = Zed.getHistoryName()
let guid:Zpool.Guid = Zpool.getGuid()

let zfsNameOption = Zfs.getNameOption()

let cmd = 
  if historyName = Zed.Create && Option.isSome zfsNameOption then
    Some (CreateZfs(guid, Zfs.getName()))
  else if historyName = Zed.Destroy then
    Some (DestroyZfs(guid, Zfs.getName()))
  else if historyName = Zed.Set then
    let (key, value) =
      Zed.getHistoryStr()
        |> fun (x:string) -> x.Split '='
        |> fun xs -> (Array.head xs, Array.last xs)

    match zfsNameOption with
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
