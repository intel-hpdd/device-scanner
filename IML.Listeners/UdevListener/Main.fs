// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.UdevListener

open Fable.Import.JS
open IML.Types.CommandTypes
open IML.Listeners.CommonLibrary

let o = JSON.stringify env

let cmd = 
  match Udev.getAction() with
    | Udev.Add -> Add o
    | Udev.Change -> Change o
    | Udev.Remove -> Remove o

let x = UdevCommand cmd

sendData x