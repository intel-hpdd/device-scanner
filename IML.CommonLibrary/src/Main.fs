// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.


module IML.CommonLibrary

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

  let private hexchar_to_int = function
    | x when x >= '0' && x <= '9' -> int x - int '0'
    | x when x >= 'A' && x <= 'F' -> int x - int 'A' + 10
    | x when x >= 'a' && x <= 'f' -> int x - int 'a' + 10
    | 'x' | 'X' -> 0
    | x -> failwithf "expected hex char, got %A" x

  let toBignumString(xs: string): string =
      xs
        |> Seq.fold (fun acc x ->
          let r = new bigint(hexchar_to_int x)

          match acc with
            | Some x -> Some (x * 16I + r)
            | None -> Some r
        ) None
        |> Option.get
        |> sprintf "%O"

