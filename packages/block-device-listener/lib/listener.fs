module BlockDeviceListener.Listener

open Fable.Core
open Fable.Import.JS
open Node.Net
open UdevEventTypes.EventTypes

let private getRecordType (x:IAction) =
  match x.ACTION with
    | Actions.Add -> Add (x :?> IAdd)
    | Actions.Remove -> Remove (x :?> IRemove)

let private toJson = getRecordType >> JsInterop.toJson

[<Pojo>]
type NetPath = {
  path: string
}

let run (net:net_types.Globals) (env:IAction) =
  let client = net.connect { path = "/var/run/device-scanner.sock"; }
  client.once(
    "connect",
    fun () -> client.``end``(toJson env)
  ) |> ignore
