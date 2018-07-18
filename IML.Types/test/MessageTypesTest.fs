// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.MessasgeTypesTest

open MessageTypes

open Fable.Import.Jest
open IML.CommonLibrary
open Matchers

test "encoding data" <| fun () ->
  Message.Data "{\"bar\":\"baz\"}"
    |> Message.encoder
    |> (==) "{\"data\":\"{\\\"bar\\\":\\\"baz\\\"}\"}"

test "decoding data" <| fun () ->
  "{\"data\":\"{\\\"bar\\\":\\\"baz\\\"}\"}"
    |> Message.decoder
    |> Result.unwrap
    |> (==) (Message.Data "{\"bar\":\"baz\"}")


test "encoding heartbeat" <| fun () ->
  Message.Heartbeat
    |> Message.encoder
    |> (==) "\"heartbeat\""

test "decoding heartbeat" <| fun () ->
  "\"heartbeat\""
    |> Message.decoder
    |> Result.unwrap
    |> (==) Message.Heartbeat
