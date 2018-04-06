// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.CommandTypes

open Fable.Core
open Thot.Json

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
  | ImportZpool of Zpool.Name * Zpool.Guid * Zpool.State
  | ExportZpool of Zpool.Guid * Zpool.State
  | DestroyZpool of Zpool.Guid
  | CreateZfs of Zpool.Guid * Zfs.Name
  | DestroyZfs of Zpool.Guid * Zfs.Name
  | SetZpoolProp of Zpool.Guid * Prop.Key * Prop.Value
  | SetZfsProp of Zpool.Guid * Zfs.Name * Prop.Key * Prop.Value
  | AddVdev of Zpool.Guid

module ZedCommand =
  let encode x =
    x
      |> function
          | Init ->
            Encode.string "Init"
          | CreateZpool ((Zpool.Name name), (Zpool.Guid guid), (Zpool.State state)) ->
            Encode.object [("CreateZpool", Encode.array [| Encode.string name; Encode.string guid; Encode.string state |])]
          | ImportZpool ((Zpool.Name name), (Zpool.Guid guid), (Zpool.State state)) ->
            Encode.object [("ImportZpool", Encode.array [| Encode.string name; Encode.string guid; Encode.string state |])]
          | ExportZpool ((Zpool.Guid guid), (Zpool.State state)) ->
            Encode.object [("ExportZpool", Encode.array [| Encode.string guid; Encode.string state |])]
          | DestroyZpool ((Zpool.Guid guid)) ->
            Encode.object [("DestroyZpool", Encode.array [| Encode.string guid; |])]
          | CreateZfs ((Zpool.Guid guid), (Zfs.Name name)) ->
            Encode.object [("CreateZfs", Encode.array [| Encode.string guid; Encode.string name |])]
          | DestroyZfs ((Zpool.Guid guid), (Zfs.Name name)) ->
            Encode.object [("DestroyZfs", Encode.array [| Encode.string guid; Encode.string name |])]
          | SetZpoolProp ((Zpool.Guid guid), key, value) ->
            Encode.object [("SetZpoolProp", Encode.array [| Encode.string guid; Encode.string key; Encode.string value |])]
          | SetZfsProp ((Zpool.Guid guid), (Zfs.Name name), key, value) ->
            Encode.object [("SetZfsProp", Encode.array [| Encode.string guid; Encode.string name; Encode.string key; Encode.string value |])]
          | AddVdev (Zpool.Guid guid) ->
            Encode.object [("AddVdev", Encode.array [| Encode.string guid; |])]
      |> fun x -> Encode.object [("ZedCommand", x)]

  let decodeInit =
    Decode.string
      |> Decode.andThen (function
          | "Init" ->
            Decode.succeed Init
          | unknown ->
            Decode.fail ("Expected ZedCommand.Init, got " + unknown)
      )

  let decodeCreateZPool =
    Decode.field "CreateZpool"
      (Decode.map3
        (fun name guid state ->
          ZedCommand.CreateZpool (Zpool.Name name, Zpool.Guid guid, Zpool.State state)
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string)
        (Decode.field "2" Decode.string))

  let decodeImportZPool =
    Decode.field "ImportZpool"
      (Decode.map3
        (fun name guid state ->
          ZedCommand.ImportZpool (Zpool.Name name, Zpool.Guid guid, Zpool.State state)
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string)
        (Decode.field "2" Decode.string))

  let decodeExportZPool =
    Decode.field "ExportZpool"
      (Decode.map2
        (fun guid state ->
          ZedCommand.ExportZpool (Zpool.Guid guid, Zpool.State state)
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string))

  let decodeDestroyZpool =
    Decode.field "DestroyZpool"
      (Decode.map (Zpool.Guid >> ZedCommand.DestroyZpool)
          (Decode.field "0" Decode.string))

  let decodeCreateZfs =
    Decode.field "CreateZfs"
      (Decode.map2
        (fun guid name ->
          ZedCommand.CreateZfs (Zpool.Guid guid, Zfs.Name name)
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string))

  let decodeDestroyZfs =
    Decode.field "DestroyZfs"
      (Decode.map2
        (fun guid name ->
          ZedCommand.DestroyZfs (Zpool.Guid guid, Zfs.Name name)
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string))

  let decodeSetZpoolProp =
    Decode.field "SetZpoolProp"
      (Decode.map3
        (fun guid key value ->
          ZedCommand.SetZpoolProp (Zpool.Guid guid, key, value)
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string)
        (Decode.field "2" Decode.string))

  let decodeSetZfsProp =
    Decode.field "SetZfsProp"
      (Decode.map4
        (fun guid name key value ->
          ZedCommand.SetZfsProp (Zpool.Guid guid, Zfs.Name name, key, value)
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string)
        (Decode.field "2" Decode.string)
        (Decode.field "3" Decode.string))

  let decodeAddVdev =
    Decode.field "AddVdev"
      (Decode.map (Zpool.Guid >> ZedCommand.AddVdev)
        (Decode.field "0" Decode.string))

  let decode =
    Decode.field "ZedCommand"
      (Decode.oneOf [
        decodeInit;
        decodeCreateZPool;
        decodeImportZPool;
        decodeExportZPool;
        decodeDestroyZpool;
        decodeCreateZfs;
        decodeDestroyZfs;
        decodeSetZpoolProp;
        decodeSetZfsProp;
        decodeAddVdev;
      ])

type UdevCommand =
  | Add of string
  | Change of string
  | Remove of string

module UdevCommand =
  let encode x =
    match x with
      | Add y ->
        Encode.object [("Add", Encode.string y)]
      | Change y ->
        Encode.object [("Change", Encode.string y)]
      | Remove y ->
        Encode.object [("Remove", Encode.string y)]
    |> fun x -> Encode.object [("UdevCommand", x)]

  let decodeAdd =
    (Decode.map UdevCommand.Add
      (Decode.field "Add" Decode.string))

  let decodeChange =
    (Decode.map UdevCommand.Change
      (Decode.field "Change" Decode.string))

  let decodeRemove =
    (Decode.map UdevCommand.Remove
      (Decode.field "Remove" Decode.string))

  let decode =
    Decode.field
      "UdevCommand"
      (Decode.oneOf [
          decodeAdd;
          decodeChange;
          decodeRemove;
      ])

[<RequireQualifiedAccess>]
module Mount =
  [<Erase>]
  type MountPoint = MountPoint of string
  [<Erase>]
  type BdevPath = BdevPath of string
  [<Erase>]
  type FsType = FsType of string
  [<Erase>]
  type MountOpts = MountOpts of string

type MountCommand =
  | AddMount of
      Mount.MountPoint * Mount.BdevPath * Mount.FsType * Mount.MountOpts
  | RemoveMount of
      Mount.MountPoint * Mount.BdevPath * Mount.FsType * Mount.MountOpts
  | ReplaceMount of
      Mount.MountPoint * Mount.BdevPath * Mount.FsType * Mount.MountOpts * Mount.MountPoint * Mount.MountOpts
  | MoveMount of
      Mount.MountPoint * Mount.BdevPath * Mount.FsType * Mount.MountOpts * Mount.MountPoint * Mount.MountOpts


module MountCommand =
  let encode x =
    match x with
    | AddMount (
        (Mount.MountPoint target),
        (Mount.BdevPath source),
        (Mount.FsType fstype),
        (Mount.MountOpts opts)
      ) ->
        Encode.object [
          (
             "AddMount",
             Encode.array [|
               Encode.string target;
               Encode.string source;
               Encode.string fstype;
               Encode.string opts
             |]
          )
        ]
    | RemoveMount (
        (Mount.MountPoint target),
        (Mount.BdevPath source),
        (Mount.FsType fstype),
        (Mount.MountOpts opts)
      ) ->
        Encode.object [
          (
             "RemoveMount",
             Encode.array [|
               Encode.string target;
               Encode.string source;
               Encode.string fstype;
               Encode.string opts
             |]
          )
        ]
    | ReplaceMount (
        (Mount.MountPoint target),
        (Mount.BdevPath source),
        (Mount.FsType fstype),
        (Mount.MountOpts opts),
        (Mount.MountPoint oldTarget),
        (Mount.MountOpts oldOpts)
      ) ->
        Encode.object [
          (
             "ReplaceMount",
             Encode.array [|
               Encode.string target;
               Encode.string source;
               Encode.string fstype;
               Encode.string opts;
               Encode.string oldTarget;
               Encode.string oldOpts
             |]
          )
        ]
    | MoveMount (
        (Mount.MountPoint target),
        (Mount.BdevPath source),
        (Mount.FsType fstype),
        (Mount.MountOpts opts),
        (Mount.MountPoint oldTarget),
        (Mount.MountOpts oldOpts)
      ) ->
        Encode.object [
          (
             "MoveMount",
             Encode.array [|
               Encode.string target;
               Encode.string source;
               Encode.string fstype;
               Encode.string opts;
               Encode.string oldTarget;
               Encode.string oldOpts
             |]
          )
        ]
    |> fun x -> Encode.object [("MountCommand", x)]

  let decodeAddMount =
    Decode.field "AddMount"
      (Decode.map4
        (fun target source fstype opts ->
          MountCommand.AddMount (Mount.MountPoint target, Mount.BdevPath source, Mount.FsType fstype, Mount.MountOpts opts)
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string)
        (Decode.field "2" Decode.string)
        (Decode.field "3" Decode.string))

  let decodeRemoveMount =
    Decode.field "RemoveMount"
      (Decode.map4
        (fun target source fstype opts ->
          MountCommand.RemoveMount (Mount.MountPoint target, Mount.BdevPath source, Mount.FsType fstype, Mount.MountOpts opts)
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string)
        (Decode.field "2" Decode.string)
        (Decode.field "3" Decode.string))

  let decodeReplaceMount =
    Decode.field "ReplaceMount"
      (Decode.map6
        (fun target source fstype opts oldTarget oldOpts ->
          MountCommand.ReplaceMount (
            Mount.MountPoint target,
            Mount.BdevPath source,
            Mount.FsType fstype,
            Mount.MountOpts opts,
            Mount.MountPoint oldTarget,
            Mount.MountOpts oldOpts
          )
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string)
        (Decode.field "2" Decode.string)
        (Decode.field "3" Decode.string)
        (Decode.field "4" Decode.string)
        (Decode.field "5" Decode.string))

  let decodeMoveMount =
    Decode.field "MoveMount"
      (Decode.map6
        (fun target source fstype opts oldTarget oldOpts ->
          MountCommand.ReplaceMount (
            Mount.MountPoint target,
            Mount.BdevPath source,
            Mount.FsType fstype,
            Mount.MountOpts opts,
            Mount.MountPoint oldTarget,
            Mount.MountOpts oldOpts
          )
        )
        (Decode.field "0" Decode.string)
        (Decode.field "1" Decode.string)
        (Decode.field "2" Decode.string)
        (Decode.field "3" Decode.string)
        (Decode.field "4" Decode.string)
        (Decode.field "5" Decode.string))

  let decode =
    Decode.field
      "MountCommand"
      (Decode.oneOf [
          decodeAddMount;
          decodeRemoveMount;
          decodeReplaceMount;
          decodeMoveMount;
      ])

type Command =
  | Stream
  | ZedCommand of ZedCommand
  | UdevCommand of UdevCommand
  | MountCommand of MountCommand


module Command =
  let encode x =
    match x with
      | Stream ->
        Encode.string "Stream"
      | ZedCommand x ->
        ZedCommand.encode x
      | UdevCommand x ->
        UdevCommand.encode x
      | MountCommand x ->
        MountCommand.encode x

  let encoder x =
    encode x
      |> Encode.encode 0

  let decodeMount =
    (Decode.map MountCommand
      MountCommand.decode)

  let decodeUdev =
    (Decode.map UdevCommand
      UdevCommand.decode)

  let decodeZed =
    (Decode.map ZedCommand
      ZedCommand.decode)

  let decodeStream =
    Decode.string
      |> Decode.andThen (function
          | "Stream" ->
            Decode.succeed Stream
          | unknown ->
            Decode.fail ("Expected Command.Stream, got " + unknown)
      )

  let decode =
    Decode.oneOf [
      decodeStream;
      decodeZed;
      decodeUdev;
      decodeMount;
    ]

  let decoder =
    Decode.decodeString decode
      >> Result.mapError exn
