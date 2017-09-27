// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.LineDelimitedJsonStream.Stream

open System
open Fable.Import.Node
open Fable.Import.JS
open Fable.Core
open Fable.PowerPack
open Fable.Core.JsInterop
open System.Text.RegularExpressions

type JsonParser = string -> Result<Json.Json, Exception>
type HandleParsedJson = Error option -> Json.Json option -> unit

type StringParser = string -> Result<string, Exception>
type HandleString = Error option -> string option -> unit

type Parsers = StringParser | JsonParser
type Handlers = HandleParsedJson | HandleString

let private jsonParser = Json.ofString
let private stringParser (x:string) = Ok(x)

let private matcher x =
  match Regex.Match(x, "\\n") with
    | m when m.Success -> Some(m.Index)
    | _ -> None

let private adjustBuff (buff:string) (index:int) =
  let out = buff.Substring(0, index)
  let buff = buff.Substring(index + 1)
  (out, buff)

let rec private getNextMatch (buff:string) (callback:Error option -> 'a option -> unit) (turn:int) (parser:Parsers) =
  let opt = matcher(buff)

  match opt with
    | None ->
      if turn = 0 then
        callback None None
      buff
    | Some(index) ->
      let (out, b) = adjustBuff buff index

      match jsonParser out with
        | Ok(x) -> callback None (Some x)
        | Error(e:exn) -> callback (!!e |> Some) None

      getNextMatch b callback (turn + 1) parser

let getStream (parser:Parsers) () =
  let mutable buff = ""

  let opts = createEmpty<Stream.TransformBufferOptions>
  opts.readableObjectMode <- Some true
  opts.transform <- (fun chunk encoding callback ->
    let self = JsInterop.jsThis

    buff <- getNextMatch
        (buff + chunk.toString("utf-8"))
        (fun err x ->
          (Option.map (fun x -> self?emit("error", x)) err)
            |> ignore

          (Option.map (fun x -> self?push(x)) x)
            |> ignore
        )
        0
        parser

    callback None None
  )

  opts.flush <- Some(fun callback ->
    if buff.Length = 0
      then
        callback(None)
      else
        let self = JsInterop.jsThis

        match jsonParser buff with
        | Ok(x) ->
          self?push x |> ignore
          callback(None)
        | Error(e:exn) -> !!e |> Some |> callback
  )

  Stream.Transform.Create<string, 'a> opts

let getJsonStream = getStream JsonParser
let getStringStream = getStream StringParser
