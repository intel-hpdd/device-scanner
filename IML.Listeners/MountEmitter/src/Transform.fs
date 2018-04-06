// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.MountEmitter.Transform

open Fable.Import.Node
open Fable.Import.Node.PowerPack
open IML.Types.CommandTypes

type Row = (string * string * string * string * string * string * string)

let private toRow = function
  // findmnt --poll -o ACTION,TARGET,SOURCE,FSTYPE,OPTIONS,OLD-TARGET,OLD-OPTIONS
  | [| a; b; c; d; e; f; g; |] -> Ok (a, b, c, d, e, f, g)
  // findmnt --list
  | [| a; b; c; d; |] -> Ok ("mount", a, b, c, d, "old-target", "old-options")
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
        let long =
          (
            Mount.MountPoint b,
            Mount.BdevPath c,
            Mount.FsType d,
            Mount.MountOpts e,
            Mount.MountPoint f,
            Mount.MountOpts g
          )
        match a with
        | "mount" -> AddMount short
        | "umount" -> RemoveMount short
        | "remount" -> ReplaceMount long
        | "move" -> MoveMount long
        | _ -> failwithf "did not get expected row, got %A" a
        |> Command.MountCommand |> Ok
    )
