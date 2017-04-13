module BlockDeviceListener.Main

open BlockDeviceListener.Listener
open UdevEventTypes.EventTypes
open Node.Net
open Node.Globals

run net (``process``.env :?> IAction)
