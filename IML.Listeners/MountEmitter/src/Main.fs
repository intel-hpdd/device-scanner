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
    |> Stream.map(fun x ->
      match x with
      | a, b, c, d, e ->
        let m =
          {
            target = Mount.MountPoint b;
            source = Mount.BdevPath c;
            fstype = Mount.FsType d;
            opts = Mount.MountOpts e
          }

        match a with
        | "mount" ->
          m
            |> Mount |> MountCommand |> Ok
        | "remount" ->
          m
            |> Remount |> MountCommand |> Ok
        | "move" ->
          m
            |> Move |> MountCommand |> Ok
        | "unmount" ->
          m
            |> Umount |> MountCommand |> Ok
        | _ ->
          failwithf "did not get expected row, got %A" a
    )


Globals.``process``.stdin
  |> transform
  |> Stream.iter sendData
  |> ignore
