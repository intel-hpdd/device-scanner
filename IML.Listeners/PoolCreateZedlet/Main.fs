// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.PoolCreateZedlet

open IML.Types.CommandTypes
open IML.Listeners.CommonLibrary

let x = ZedCommand (CreateZpool (Zed.getZpoolName(), Zed.getGuid(), Zed.getState())) 

sendData x
