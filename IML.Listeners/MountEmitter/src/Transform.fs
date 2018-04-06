// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.MountEmitter.Transform

open Fable.Import.Node
open Fable.Import.Node.PowerPack
open IML.Types.CommandTypes

type Row = (string *  string * string * string * string)

let private toRow = function
  | [| a; b; c; d; e; |] -> Ok (a, b, c, d, e)
  | [| a; b; c; d; |] -> Ok ("mount", a, b, c, d)
  | x -> failwithf "did not get expected row data, got %A" x

let private notHeader = function
  | (_, "TARGET", "SOURCE", "FSTYPE", "OPTIONS") -> false
  | _ -> true

let transform (x:Stream.Readable<string>) =
  x
    |> Stream.map (buffer.Buffer.from >> Ok)
    |> Stream.LineDelimited.create()
    |> Stream.map (fun x -> Ok (x.Split([| ' ' |], System.StringSplitOptions.RemoveEmptyEntries)))
    |> Stream.map toRow
    |> Stream.filter (notHeader >> Ok)
    |> Stream.map(function
      | a, b, c, d, e ->
        match a with
        | "mount" -> AddMount (Mount.MountPoint b, Mount.BdevPath c, Mount.FsType d, Mount.MountOpts e)
        | "umount" -> RemoveMount (Mount.MountPoint b, Mount.BdevPath c, Mount.FsType d, Mount.MountOpts e)
        // | "remount" -> toCommand Remount m
        // | "move" -> toCommand Movemount m
        | _ -> failwithf "did not get expected row, got %A" a
        |> Command.MountCommand |> Ok
    )
