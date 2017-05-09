// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module BlockDeviceListener.Main

open BlockDeviceListener.Listener
open UdevEventTypes.EventTypes
open Fable.Import.Node.Net
open Fable.Import.Node.Globals

run net (``process``.env :?> IAction)
