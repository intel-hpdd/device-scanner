module IML.IntegrationTest.StatefulPromise

open Fable.Import
open Fable.PowerPack

type StatefulResult<'s, 'a, 'e> = Result<('a * 's), ('e * 's)>
type StatefulPromiseResult<'s, 'a, 'e> = JS.Promise<StatefulResult<'s, 'a, 'e>>
type StateS<'s, 'a, 'e> = 's -> StatefulPromiseResult<'s, 'a, 'e>

let run<'s, 'a, 'e> (state:'s) (fn:StateS<'s, 'a, 'e>) : StatefulPromiseResult<'s, 'a, 'e> =
  fn state

let rtrn x = fun s -> Promise.lift (Ok (x, s)) : JS.Promise<Result<('a * 'b), 'c>>

let (>>=) (rest:'a -> StateS<'s, 'c, 'b>) (s1:StateS<'s, 'a, 'b>) : StateS<'s, 'c, 'b> =
  fun state ->
    run state s1
      |> Promise.bind (fun r ->
        match r with
          | Ok (a, s) -> rest a s
          | Error (e, s) -> Promise.lift (Error (e, s))
      )

type StatefulPromise() =
  member __.Bind(x, y) = y >>= x
  member __.Return(x) = rtrn x
  member __.Zero () = rtrn ()
