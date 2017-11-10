module IML.MaybeTest

open IML.Maybe
open Fable.Import.Jest
open Matchers

test "Some" <| fun () ->
  let x = maybe {
    let! y = Some(5)

    return y + 5
  }

  x == Some(10)

test "None" <| fun () ->
  let x = maybe {
    let! y = None

    return y + 5
  }

  x == None

test "Return Some" <| fun () ->
  let x = maybe {
    let! y = Some(4);

    return! Some(6 + y)
  }

  x == Some 10
