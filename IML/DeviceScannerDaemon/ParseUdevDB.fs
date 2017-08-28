module IML.DeviceScannerDaemon.ParseUdevDB

open Fable.PowerPack
open Fable.Import
open Fable.Core.JsInterop
open Fable.Import.Node
open Fable.Core

let private opts = createEmpty<ChildProcess.ExecOptions>

let getUdevDB () =
  Promise.create(fun res rej ->
    ChildProcess.exec("udevadm info -e", opts, (fun e stdout stderr ->
      match e with
        | Some(e) ->
          JS.console.error(stderr)
          rej(!!e)
        | None ->
          match stdout with
            | U2.Case1(x) -> res x
            | U2.Case2(x) -> res(x.toString "utf8")
    ))
      |> ignore
  )

let parser (x:string) =
  x.Split([| "\n\n" |], System.StringSplitOptions.None)
    |> Array.filter(fun x -> x.IndexOf("SUBSYSTEM=block") > 0)
    |> Array.map(
      fun x -> x.Split('\n')
      >> Array.filter(fun x -> x.StartsWith("E: "))
      >> Array.map(fun x -> x.Substring(3))
      >> Array.map(fun x -> x.Split('='))
      >> Array.map(function
        | [| key; value; |] -> (key, Json.String value)
        | _ -> failwith "incorrect params when parsing udev info"
      )
      >> Map.ofArray
    )
