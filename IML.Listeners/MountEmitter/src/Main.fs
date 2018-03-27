// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.MountEmitter

open IML.Listeners.CommonLibrary
open Fable.Import.Node
open Fable.Import.Node.PowerPack
open IML.Types.CommandTypes

type Row = (string *  string * string * string * string)

let toRow = function
  | [| a; b; c; d; e; |] -> Ok (a, b, c, d, e)
  | [| a; b; c; d; |] -> Ok ("mount", a, b, c, d)
  | x -> failwithf "did not get expected row data, got %A" x

let header = ("ACTION", "TARGET", "SOURCE", "FSTYPE", "OPTIONS")
let notHeader x = x <> header

let transform (x:Stream.Readable<string>) =
  x
    |> Stream.map (buffer.Buffer.from >> Ok)
    |> Stream.LineDelimited.create()
    |> Stream.map (fun x -> Ok (x.Split([| ' ' |], System.StringSplitOptions.RemoveEmptyEntries)))
    |> Stream.map toRow
    |> Stream.filter (notHeader >> Ok)
    |> Stream.map(fun x ->  //function
      match x with
      | a, b, c, d, e ->
        match a with
        | "mount" ->
          Command.MountCommand (Mount (Mount.MountPoint b, Mount.BdevPath c, Mount.FsType d, Mount.Options e))
            |> Ok
        | "umount" ->
          Command.MountCommand (Umount (Mount.MountPoint b))
            |> Ok
        // | "reount" ->
          // Command.MountCommand (Reount (Mount.MountPoint b, Mount.BdevPath c, Mount.FsType d, Mount.Options e))
            // |> Ok
        // | "move" ->
          // Command.MountCommand (Move (Mount.MountPoint b, Mount.BdevPath c, Mount.FsType d, Mount.Options e))
            // |> Ok
          // Move (MountPoint, BdevPath, FsType, Options)
        | _ -> failwithf "did not get expected row, got %A" a
    )


Globals.``process``.stdin
  |> transform
  |> Stream.iter sendData
  |> ignore

// mount      /mnt/fs-OST0002 /dev/sdd lustre ro