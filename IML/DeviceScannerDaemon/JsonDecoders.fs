// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.JsonDecoders

open Fable.PowerPack

let object = function
  | Json.Object a -> Some (Map.ofArray a)
  | _ -> None

let str = function
  | Json.String a -> Some a
  | _ -> None

let unwrapString a =
    match a with
    | Json.String a -> a
    | _ -> failwith "Invalid JSON, it must be a string"

let findOrFail (key:string) x =
  match Map.tryFind key x with
    | Some(x) -> unwrapString x
    | None -> failwith (sprintf "Could not find key %s in %O" key x)

let findOrNone key x =
  x |> Map.tryFind key |> Option.bind str
