module IML.Maybe

type MaybeBuilder() =
    member this.Bind(x, f) =
        match x with
        | Some(x) -> f(x)
        | _ -> None
    member this.Delay(f) = f()
    member this.Return(x) = Some x

    member this.ReturnFrom(x) = x

let maybe = MaybeBuilder();
