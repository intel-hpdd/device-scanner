// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.CommandTypesTest

open Fable.Import.Jest
open Fable.Core.JsInterop
open Fable.Import
open Matchers
open CommandTypes
open Thot.Json

let encoder enc x =
  enc x
    |> Encode.encode 2

let decoder dec =
  dec
    |> Decode.decodeString

let zedEncode = encoder ZedCommand.encode
let zedDecode = decoder ZedCommand.decode

let zedTestPrefix = "encoding ZED commands"

let zedCommands = [
    ("Init", ZedCommand.Init);
    ("CreateZpool", ZedCommand.CreateZpool (Zpool.Name "test", Zpool.Guid "0x6b289bd5ee51b853", Zpool.State "ACTIVE"));
    ("ImportZPool", ZedCommand.ImportZpool (Zpool.Name "test", Zpool.Guid "0x6b289bd5ee51b853", Zpool.State "ACTIVE"));
    ("ExportZPool", ZedCommand.ExportZpool (Zpool.Guid "0x6b289bd5ee51b853", Zpool.State "EXPORTED"));
    ("DestroyZPool", ZedCommand.DestroyZpool (Zpool.Guid "0x6b289bd5ee51b853"));
    ("CreateZfs", ZedCommand.CreateZfs (Zpool.Guid "0x6b289bd5ee51b853", Zfs.Name "test/ds"));
    ("DestroyZfs", ZedCommand.DestroyZfs (Zpool.Guid "0x6b289bd5ee51b853", Zfs.Name "test/ds"));
    ("SetZpoolProp", ZedCommand.SetZpoolProp (Zpool.Guid "0x6b289bd5ee51b853", "lustre:mgsnode", "10.14.82.0@tcp:10.14.82.1@tcp"));
    ("AddVdev", ZedCommand.AddVdev (Zpool.Guid "0x6b289bd5ee51b853"));
]

zedCommands
  |> List.map (fun ((name, cmd)) ->
    Test(name, fun () ->
      cmd
        |> zedEncode
        |> toMatchSnapshot
    )
  )
  |> testList zedTestPrefix

let udevEncode = encoder UdevCommand.encode
let udevDecode = decoder UdevCommand.decode

let udevTestPrefix = "encoding Udev commands"

let udevCommands = [
  ("Add", UdevCommand.Add "\"foo\"");
  ("Change", UdevCommand.Change "\"foo\"");
  ("Remove", UdevCommand.Change "\"foo\"");
]

udevCommands
  |> List.map (fun ((name, cmd)) ->
    Test(name, fun () ->
      cmd
        |> udevEncode
        |> toMatchSnapshot
    )
  )
  |> testList udevTestPrefix

let commandTestPrefix = "encoding Commands"

let commands = [
  ("Stream", Command.Stream)
  ("ZedCommand", Command.ZedCommand ZedCommand.Init);
  ("UdevCommand", Command.UdevCommand (UdevCommand.Add "\"foo\""));
]

commands
  |> List.map (fun ((name, cmd)) ->
    Test(name, fun () ->
      cmd
        |> Command.encoder
        |> toMatchSnapshot
    )
  )
  |> testList commandTestPrefix

// Reuse our snapshots to test decoding
let snaps:obj = importAll "./__snapshots__/CommandTypesTest.fs.snap"

zedCommands
  |> List.map (fun ((name, cmd)) ->
    let caseName = sprintf "%s %s 1" zedTestPrefix name
    let o:string =
      !!snaps?(caseName)
      |> String.filter (fun x -> x <> '\n')

    Test(name, fun () ->
      zedDecode !!(JS.JSON.parse o) == Ok cmd
    )
  )
  |> testList "decoding ZED commands"

udevCommands
  |> List.map (fun ((name, cmd)) ->
    let caseName = sprintf "%s %s 1" udevTestPrefix name

    let o:string =
      !!snaps?(caseName)
      |> String.filter (fun x -> x <> '\n')

    Test(name, fun () ->
      udevDecode !!(JS.JSON.parse o) == Ok cmd
    )
  )
  |> testList "decoding Udev commands"

commands
  |> List.map (fun ((name, cmd)) ->
    let caseName = sprintf "%s %s 1" commandTestPrefix name

    let o:string =
      !!snaps?(caseName)
      |> String.filter (fun x -> x <> '\n')

    Test(name, fun () ->
      Command.decoder !!(JS.JSON.parse o) == Ok cmd
    )
  )
  |> testList "decoding Commands"
