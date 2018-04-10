// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.MountEmitter.Transform

open Fable.Import.Node
open Fable.Import.Node.PowerPack
open IML.Types.CommandTypes
open Fable.Import.Node.Base.NodeJS

// findmnt --poll -o ACTION,TARGET,SOURCE,FSTYPE,OPTIONS,OLD-TARGET,OLD-OPTIONS
type Row = (string * string * string * string * string * string * string)

let private toRow = function
  // findmnt --list (record or header)
  | [| a; b; c; d; |] -> Ok ("mount", a, b, c, d, "", "")
  // mount
  | [| a; b; c; d; e; |] -> Ok (a, b, c, d, e, "", "")
  // remount or move
  | [| a; b; c; d; e; f; |] -> Ok (a, b, c, d, e, f, "")
  // umount or header
  | [| a; b; c; d; e; f; g; |] -> Ok (a, b, c, d, e, f, g)
  | x -> failwithf "did not get expected row data, got %A" x

let private notHeader = function
  | (_, "TARGET", "SOURCE", "FSTYPE", "OPTIONS", _, _) -> false
  | _ -> true

let transform (x:Stream.Readable<string>) =
  x
    |> Stream.map (buffer.Buffer.from >> Ok)
    |> Stream.LineDelimited.create()
    |> Stream.map (fun x -> Ok (x.Split([| ' ' |], System.StringSplitOptions.RemoveEmptyEntries)))
    |> Stream.map toRow
    |> Stream.filter (notHeader >> Ok)
    |> Stream.map(function
      | a, b, c, d, e, f, g ->
        let short =
          (
            Mount.MountPoint b,
            Mount.BdevPath c,
            Mount.FsType d,
            Mount.MountOpts e
          )
        match a with
        | "mount" ->
          AddMount short
            |> Command.MountCommand |> Ok
        | "umount" ->
          RemoveMount short
            |> Command.MountCommand |> Ok
        | "remount" ->
          ReplaceMount
            (
              Mount.MountPoint b,
              Mount.BdevPath c,
              Mount.FsType d,
              Mount.MountOpts e,
              Mount.MountOpts f
            )
            |> Command.MountCommand |> Ok
        | "move" ->
          MoveMount
            (
              Mount.MountPoint b,
              Mount.BdevPath c,
              Mount.FsType d,
              Mount.MountOpts e,
              Mount.MountPoint f
            )
            |> Command.MountCommand |> Ok
        //| e -> Error
        // sprintf "did not get expected row, got %A" a |> Error
        | _ ->
          failwithf "did not get expected row, got %A" a
    )
