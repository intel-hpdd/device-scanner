// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.MessageTypes

open Thot.Json

type Message =
  | Data of string
  | Heartbeat

module Message =
  let encode = function
    | Data x ->
      Encode.object [ ("data", Encode.string x) ]
    | Heartbeat ->
      Encode.string "heartbeat"

  let encoder =
    encode
      >> Encode.encode 0

  let decodeHeartbeat =
    Decode.string
      |> Decode.andThen (function
        | "heartbeat" ->
          Decode.succeed Message.Heartbeat
        | unknown ->
          Decode.fail ("Expected heartbeat, got " + unknown)
      )

  let decodeData =
    (Decode.map Message.Data
      (Decode.field "data" Decode.string))

  let decode =
    (Decode.oneOf
      [
        decodeHeartbeat;
        decodeData;
      ])

  let decoder =
    Decode.decodeString decode
      >> Result.mapError exn
