// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.PoolExportZedlet

open IML.Types.CommandTypes
open IML.Listeners.CommonLibrary

let x = ZedCommand (ExportZpool (Zpool.getGuid(), Zpool.getState())) 

sendData x