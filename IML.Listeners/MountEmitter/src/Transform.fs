// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.MountEmitter.Transform

open Fable.Import.Node
open Fable.Import.Node.PowerPack
open IML.Types.CommandTypes
open Fable

let private toError =
  exn >> Error

let private toSuccess =
  Command.MountCommand >> Ok

let private toMap =
  Array.fold (fun acc (x:string) ->
    let idx = x.IndexOf "="
    let k = x.Substring(0, idx)
    let v = x.Substring(idx + 1).Trim('"')

    Map.add k v acc
  ) Map.empty
  >> Ok

let transform (x:Stream.Readable<string>) =
  x
    |> Stream.map (buffer.Buffer.from >> Ok)
    |> Stream.LineDelimited.create()
    |> Stream.map (fun x -> Ok (x.Split([| ' ' |], System.StringSplitOptions.RemoveEmptyEntries)))
    |> Stream.map toMap
    |> Stream.map (fun m ->
      let short =
        (
          Mount.MountPoint (Map.find "TARGET" m),
          Mount.BdevPath (Map.find "SOURCE" m),
          Mount.FsType (Map.find "FSTYPE" m),
          Mount.MountOpts (Map.find "OPTIONS" m)
        )
      let (target, source, fstype, opts) = short
      match m.TryFind "ACTION" with
      | Some "mount" -> AddMount short |> toSuccess
      | Some "umount" -> RemoveMount short |> toSuccess
      | Some "remount" ->
        ReplaceMount (
          target, source, fstype, opts,
          Mount.MountOpts (Map.find "OLD-OPTIONS" m)
        ) |> toSuccess
      | Some "move" ->
        MoveMount (
          target, source, fstype, opts,
          Mount.MountPoint (Map.find "OLD-TARGET" m)
        ) |> toSuccess
      // no ACTION key is populated when --poll option is not used
      | None -> AddMount short |> toSuccess
      | Some _ ->
        toError (sprintf "unexpected action type, received %A" x)
    )
