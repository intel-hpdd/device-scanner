// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.PoolImportZedlet

open IML.Types.CommandTypes
open IML.Listeners.CommonLibrary

let x = ZedCommand (ImportZpool (Zpool.getName(), Zpool.getGuid(), Zpool.getState()))

sendData x