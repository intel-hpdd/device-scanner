module IML.ScannerProxyDaemon.ProxyHandlersTest

open TestFixtures
open Fable.Import.Jest
open Matchers
open ProxyHandlers
open Fable.PowerPack

testList "Data Handler" [
  Test("Should return result of buffer on incoming Json", fun () ->
    updateJson
      |> dataHandler
      |> Result.unwrapResult
      |> (fun x -> x.toString())
      |> toMatchSnapshot
  )

  Test("Should filter out all but the base64 encoded string of 'server'", fun () ->
    [| "foo"; "bar"; "c2VydmVy" |]
      |> (filterFileName "server")
      |> Seq.toArray
      |> toMatchSnapshot
  )

  Test("Should parse url from map", fun () ->
    Map.empty
      |> Map.add "url" "https://foo.com:443/agent/"
      |> parseUrl
      |> toMatchSnapshot
  )
]
