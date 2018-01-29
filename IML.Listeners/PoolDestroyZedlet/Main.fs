// Copyright (c) 2018 Intel Corporation. All rights reserved. 
// Use of this source code is governed by a MIT-style 
// license that can be found in the LICENSE file. 

module IML.PoolDestroyZedlet

open IML.Types.CommandTypes
open IML.Listeners.CommonLibrary

let x = ZedCommand (DestroyZpool (Zed.getGuid())) 

sendData x