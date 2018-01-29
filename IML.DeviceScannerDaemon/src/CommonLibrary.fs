// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 
 

module IML.DeviceScannerDaemon.CommonLibrary 

[<RequireQualifiedAccess>]
module Option =
  let expect message = function
    | Some x -> x
    | None -> failwith message

[<RequireQualifiedAccess>]
module String =
  let startsWith (x:string) (y:string) = y.StartsWith(x)
  let endsWith (x:string) (y:string) = y.EndsWith(x)
  let split (x:char) (s:string) = s.Split(x)
  let trim (y:string) = y.Trim()
  let emptyStrToNone x = if x = "" then None else Some(x)
 
type MaybeBuilder() = 
    member __.Bind(x, f) = 
        match x with 
        | Some(x) -> f(x) 
        | _ -> None 
    member __.Delay(f) = f() 
    member __.Return(x) = Some x 
    member __.ReturnFrom(x) = x 
 
let maybe = MaybeBuilder(); 