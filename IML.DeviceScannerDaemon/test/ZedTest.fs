// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.DeviceScannerDaemon.ZedTest

open IML.DeviceScannerDaemon
open TestFixtures
open Zed
open Fable.Import.Jest
open Matchers

let poolCreateMatch = function
  | Zpool.Create x -> Some x
  | _ -> None

let poolDestroyMatch = function
  | Zpool.Destroy x -> Some x
  | _ -> None

let poolExportMatch = function
  | Zpool.Export x -> Some x
  | _ -> None

let poolImportMatch = function
  | Zpool.Import x -> Some x
  | _ -> None

let datasetCreateMatch = function
  | Zfs.Create x -> Some x
  | _ -> None

let datasetDestroyMatch = function
  | Zfs.Destroy x -> Some x
  | _ -> None

test "Matching Events" <| fun () ->
  expect.assertions 6

  toMatchSnapshot (poolCreateMatch createZpoolJson)

  toMatchSnapshot (poolDestroyMatch destroyZpoolJson)

  toMatchSnapshot (poolExportMatch exportZpoolJson)

  toMatchSnapshot (poolImportMatch importZpoolJson)

  toMatchSnapshot (datasetCreateMatch createZdatasetJson)

  toMatchSnapshot (datasetDestroyMatch destroyZdatasetJson)
