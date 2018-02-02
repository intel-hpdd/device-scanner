module IML.StatefulMonad.StatefulPromiseTest

open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open Fable.PowerPack

open IML.StatefulMonad.StatefulPromise

let command = StatefulPromise()

let createStatefulPromise (r:Result<'a, 'e>) (s:int) =
  match r with
    | Ok x -> Promise.lift(Ok(x, s + 1))
    | Error e -> Promise.lift(Error(e, s))

let testRun state fn =
  promise {
    let! runResult = run state fn
    match runResult with
      | Ok(_, count) ->
        return count
      | Error(_, count) ->
        return count
  }

Exports.testAsync "Stateful Promise should increment count until it receives an error" <| fun () ->
  command {
    let! _ = createStatefulPromise (Ok("command1"))
    let! _ = createStatefulPromise (Ok("command2"))
    let! _ = createStatefulPromise (Error("command3"))
    let! r = createStatefulPromise (Ok("command4"))

    return r
  } |> testRun 5
  |> Promise.map(fun x ->
    x == 7
  )
