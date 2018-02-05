module IML.StatefulPromise.StatefulPromiseTest

open Fable.Import.Jest
open Matchers
open Fable.PowerPack

open IML.StatefulPromise.StatefulPromise

let success () s = StatefulPromise.rtrn () (s + 1)
let err () s = Promise.lift (Error ((), s))


testAsync "Stateful Promise should increment count until it receives an error" <| fun () ->
  command {
    do! success()
    do! success()
    do! err()
    do! success()
  } |> run 5
  |> Promise.map(function
    | Ok (_, s) -> s
    | Error (_, s) -> s
  )
  |> Promise.map(toEqual 7)
