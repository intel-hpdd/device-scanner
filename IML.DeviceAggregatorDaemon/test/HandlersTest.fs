// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceAggregatorDaemon.HandlersTest

open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Jest
open Fable.Import.Node
open Fable.Import.Node.PowerPack.Stream
open IML.Types.MessageTypes
open IML.CommonLibrary
open Matchers
open Heartbeats
open Handlers
open IML.Types.Fixtures
open IML.Types
open Thoth.Json

let testServerHost = "localhost"
let testServerPort = 8181

/// scanner output forwarded from the proxy
let updatePayloadWith x =
    x |> (Data
          >> Message.encoder
          >> Some)

let updatePayload = updatePayloadWith fixtures.scannerState

let heartbeatPayload =
    Heartbeat
    |> Message.encoder
    |> Some

test "host has pool disks"
<| fun () ->
    toEqual
        (matchPaths [ "/foo/bar"; "/foo/baz"; "/bar/baz" ]
             [ "/foo/bar"; "/bar/baz" ], true) |> ignore
test "host doesn't have pool disks"
<| fun () ->
    toEqual
        (matchPaths [ "/foo/bar"; "/foo/baz"; "/bar/baz" ]
             [ "/foo/bar"; "/bar/boz" ], true) |> ignore
test "Discover no pools on host"
<| fun () ->
    discoverZpools "ffo.com" Map.empty Map.empty List.empty |> toMatchSnapshot
testList "Server"
    [ let withSetup f (d : Jest.Bindings.DoneStatic) : unit =
          // bring up server to run test target handler
          let server = http.createServer(serverHandler).listen(testServerPort)

          // request builder for requests to send to test server
          let request (method, onData, originHost, lastRequest, headers) =
              let opts = createEmpty<Https.RequestOptions>
              opts.hostname <- Some testServerHost
              opts.port <- Some testServerPort
              opts.method <- Some method
              opts.rejectUnauthorized <- Some false
              match headers with
              | Some x -> opts.headers <- Some x
              | None ->
                  opts.headers <- createObj
                                      [ "Content-Type" ==> "application/json"
                                        "x-ssl-client-name" ==> originHost ]
                                  |> Some
              http.request (opts,
                            fun r ->
                                r
                                |> reduce ""
                                       (fun acc x ->
                                       Ok(acc + x.toString ("utf-8")))
                                |> iter (fun x ->
                                    x
                                    |> Decode.decodeString (Decode.dict LegacyTypes.LegacyDevTree.decode)
                                    |> Result.unwrap
                                    |> Map.mapValues LegacyTypes.LegacyDevTree.encode
                                    |> Encode.dict
                                    |> Encode.encode 2
                                    |> onData
                                )
                                |> Writable.onFinish (fun () ->
                                       if lastRequest then
                                           server.close() |> ignore
                                           d.``done``())
                                |> ignore)
              |> Readable.onError (printfn "error when connecting: %A")

          // send get request and match snapshot on response, terminate test server
          let getRequest() =
              request (Http.Methods.Get, toMatchSnapshot, "foo.com", true, None)
              |> Writable.``end`` None
          // send post request but don't terminate test server
          let postRequest host headers =
              request (Http.Methods.Post, ignore, host, false, headers)

          // send post request with update data and when response is finished, send get to request
          let postThenGet payload =
              postRequest "foo.com" None
              |> Writable.onFinish getRequest
              |> Writable.``end`` payload

          // send patch request but don't terminate test server
          let patchRequest host headers =
              request (Http.Methods.Patch, ignore, host, false, headers)
          f getRequest postRequest postThenGet patchRequest
          heartbeats <- Map.empty
          devTree <- Map.empty
      yield! testFixtureDone withSetup
                 [ "should receive empty tree in get response without prior update",
                   fun get _ _ _ ->
                       expect.assertions 1
                       get()
                   "should receive updated tree in get response after post with update",
                   fun _ _ postThenGet _ ->
                       expect.assertions 1
                       postThenGet updatePayload
                   "should parse MdRaid correctly",
                   fun _ _ postThenGet _ ->
                       expect.assertions 1
                       postThenGet (updatePayloadWith fixtures.scannerStateMdRaid);
                   "should receive empty tree in get response after post with update but no header entry",
                   fun get post _ _ ->
                       expect.assertions 1
                       let headers =
                           createObj [ "Content-Type" ==> "application/json" ]
                       post "foo.com" (Some headers)
                       |> Writable.onFinish get
                       |> Writable.``end`` updatePayload
                   "should receive empty tree in get response after post with update but empty hostname",
                   fun get post _ _ ->
                       expect.assertions 1
                       post "" None
                       |> Writable.onFinish get
                       |> Writable.``end`` updatePayload
                   "should receive empty tree in get response after post with heartbeat",
                   fun _ _ postThenGet _ ->
                       expect.assertions 1
                       jest.useFakeTimers() |> ignore
                       postThenGet heartbeatPayload
                       jest.runAllTimers()
                   "should receive empty tree in get response after patch with update",
                   fun get _ _ patch ->
                       expect.assertions 1
                       patch "foo.com" None
                       |> Writable.onFinish get
                       |> Writable.``end`` updatePayload
                   "should receive updated tree in get response after updates from multiple hosts",
                   fun _ post postThenGet _ ->
                       expect.assertions 1
                       post "bar.com" None
                       |> Writable.onFinish
                              (fun () -> postThenGet updatePayload)
                       |> Writable.``end`` updatePayload

                   "should receive tree after updates from multiple hosts with shared pools",
                   fun _ post postThenGet _ ->
                       expect.assertions 1
                       post "bar.com" None
                       |> Writable.onFinish
                              (fun () ->
                              postThenGet
                                  (updatePayloadWith fixtures.scannerState3))
                       |> Writable.``end``
                              (updatePayloadWith fixtures.scannerState2)

                   "should receive tree after updates from multiple hosts with shared datasets",
                   fun _ post postThenGet _ ->
                       expect.assertions 1
                       post "bar.com" None
                       |> Writable.onFinish
                              (fun () ->
                              postThenGet
                                  (updatePayloadWith
                                       fixtures.scannerStateDatasets2))
                       |> Writable.``end``
                              (updatePayloadWith fixtures.scannerStateDatasets) ] ]
