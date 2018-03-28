// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.


module IML.CommonLibrary

open System

[<RequireQualifiedAccess>]
module Result =
  let unwrap = function
    | Ok x -> x
    | Error _ -> failwithf "Tried to unwrap a result, but got an error."

  let bindError (f: 'a -> Result<'c, 'b>) (x: Result<'c, 'a>) =
    match x with
      | Ok x -> Ok x
      | Error x -> f x

  let apply fn x =
    match fn,x with
      | Ok f, Ok x -> f x |> Ok
      | Error e, _ | _, Error e -> Error e
  let isOk = function
    | Ok _ -> true
    | Error _ -> false

  let isError x =
    x
      |> isOk
      |> not

[<RequireQualifiedAccess>]
module Option =
  let toResult e = function
    | Some x -> Ok x
    | None -> Error e

[<RequireQualifiedAccess>]
module String =
  let startsWith (x:string) (y:string) = y.StartsWith(x)
  let endsWith (x:string) (y:string) = y.EndsWith(x)
  let split (x:char) (s:string) = s.Split(x)
  let trim (y:string) = y.Trim()
  let emptyStrToNone x = if x = "" then None else Some(x)

type MaybeBuilder() =
    member __.Bind(x, f) = Option.bind f x
    member __.Delay(f) = f()
    member __.Return(x) = Some x
    member __.ReturnFrom(x) = x

let maybe = MaybeBuilder();

[<RequireQualifiedAccess>]
module Hex =
  let toUint64String(x: string): string =
    Convert.ToUInt64(x, 16)
        |> sprintf "%O"

