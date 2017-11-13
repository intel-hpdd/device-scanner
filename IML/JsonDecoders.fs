// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.JsonDecoders

open Fable.PowerPack
open Maybe

let unwrapObject = function
  | Json.Object a -> Map.ofArray a
  | _ -> failwith "Invalid JSON, it must be an object"

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

let findJson (fn:Json.Json -> 'b) (key:string) x =
  match Map.tryFind key x with
    | Some(x) -> fn x
    | None -> failwith (sprintf "Could not find key %s in %O" key x)

let tryFindJson fn key x =
  maybe {
    let! v = Map.tryFind key x

    return! fn v
  }
