// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.MountEmitter

open IML.Listeners.CommonLibrary
open Fable.Import.Node
open Fable.Import.Node.PowerPack

type Row = (string *  string * string * string * string)

let toRow = function
  | [| a; b; c; d; e; |] -> Ok (a, b, c, d, e)
  | x -> failwithf "did not get expected row data, got %A" x

let header = ("ACTION", "TARGET", "SOURCE", "FSTYPE", "OPTIONS")
let notHeader x = x <> header

Globals.``process``.stdin
  |> Stream.map (buffer.Buffer.from >> Ok)
  |> Stream.LineDelimited.create()
  |> Stream.map (fun x -> Ok (x.Split ' ')) // replace with split
  |> Stream.map toRow
  |> Stream.filter (notHeader >> Ok)
  |> Stream.map(fun x ->  //function
    printf "output %A" x
    x |> Ok
  )
  |> Stream.iter sendData
  |> ignore












// mount      /mnt/fs-OST0002 /dev/sdd lustre ro
