// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.IntegrationTest.IntegrationTest

open Fable.Import.Jest.Matchers
open IML.StatefulPromise.StatefulPromise
open IML.IntegrationTestFramework.IntegrationTestFramework
open Fable.Import.Jest
open Fable.Import.Node.PowerPack.ChildProcess

let udevAdmTrigger =
  cmd "udevadm trigger"

let scannerInfo =
  pipeToShellCmd "echo '\"Info\"'" "socat - UNIX-CONNECT:/var/run/device-scanner.sock"

testAsync "info event" <| fun () ->
  command {
        do! udevAdmTrigger >> ignoreCmd
        let! (Stdout(x), _) = scannerInfo

        toMatchSnapshot x
      } |> run []
