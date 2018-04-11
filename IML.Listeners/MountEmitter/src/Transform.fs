// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.MountEmitter.Transform

open Fable.Import.Node
open Fable.Import.Node.PowerPack
open IML.Types.CommandTypes
open IML.CommonLibrary

let private toError =
  exn >> Error

let private columnError = function
  | x -> toError (sprintf "did not find expected column values for '%A' action" x)

let private toSuccess =
  Command.MountCommand >> Ok

let private toMap (xs:string []) =
  let mutable pairs = Map.empty
  let splitChar = '='

  xs
    |> Array.map (fun x ->
      x
        |> fun (x:string) -> x.Split splitChar
        |> fun xs ->
          let (first, remainder) = Array.splitAt 1 xs
          let remainder = Array.reduce (fun acc item -> acc + (string splitChar) + item) remainder

          pairs <- Map.add (Array.head first) (remainder.Trim '"') pairs
    ) |> ignore

  pairs |> Ok

let transform (x:Stream.Readable<string>) =
  x
    |> Stream.map (buffer.Buffer.from >> Ok)
    |> Stream.LineDelimited.create()
    |> Stream.map (fun x -> Ok (x.Split([| ' ' |], System.StringSplitOptions.RemoveEmptyEntries)))
    |> Stream.map toMap
    |> Stream.map (fun m ->
      let short =
        (
          Mount.MountPoint (Option.get (m.TryFind "TARGET")),
          Mount.BdevPath (Option.get (m.TryFind "SOURCE")),
          Mount.FsType (Option.get (m.TryFind "FSTYPE")),
          Mount.MountOpts (Option.get (m.TryFind "OPTIONS"))
        )
      let (target, source, fstype, opts) = short
      match m.TryFind "ACTION" with
      | Some x ->
        match x with
        | "mount" -> AddMount short |> toSuccess
        | "umount" -> RemoveMount short |> toSuccess
        | "remount" ->
          match m.TryFind "OLD-TARGET" with
          | Some y ->
            ReplaceMount
              (
                target, source, fstype, opts,
                Mount.MountOpts y
              ) |> toSuccess
          | None -> columnError x
        | "move" ->
          match m.TryFind "OLD-TARGET" with
          | Some y ->
            MoveMount
              (
                target, source, fstype, opts,
                Mount.MountPoint y
              ) |> toSuccess
          | None -> columnError x
        | _ ->
          toError (sprintf "unexpected action type, received %A" x)
      // no ACTION key is populated when --poll option is not used
      | None -> AddMount short |> toSuccess
    )
