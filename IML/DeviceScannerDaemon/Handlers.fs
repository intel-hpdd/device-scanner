// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Handlers

open Fable.Import.Node.PowerPack.Stream
open Udev
open Zed

open libzfs

module Option =
  let expect message = function
    | Some x -> x
    | None -> failwith message

let private scan init update =
  let mutable state = init()

  fun (x) ->
    state <- update state x
    state

type Data = {
  blockDevices: Map<DevPath, UEvent>;
  zpools: Map<Zpool.Guid, Zpool.Data>;
  zfs: Map<Zfs.Id, Zfs.Data>;
  props: Properties.Property list;
}

let (|Info|_|) (x:LineDelimitedJson.Json) =
  match actionDecoder x with
    | Ok(y) when y = "info" -> Some()
    | _ -> None

let (|ZedTrigger|_|) (x:LineDelimitedJson.Json) =
  match actionDecoder x with
    | Ok(y) when y = "trigger zed" -> Some()
    | _ -> None

let private toMap key xs =
  let folder state x =
    Map.add (key x) x state

  Seq.fold folder Map.empty xs

let init () =
  {
    blockDevices = Map.empty;
    zpools = Map.empty;
    zfs = Map.empty;
    props = [];
  }

let update (state:Data) (x:LineDelimitedJson.Json):Data =
  match x with
    | Info -> 
      state
    | UdevAdd x | UdevChange x ->
      { state with blockDevices = Map.add x.DEVPATH x state.blockDevices }
    | UdevRemove x ->
      { state with blockDevices = Map.remove x.DEVPATH state.blockDevices }
    | ZedTrigger ->
      let libzfsPools = 
        libzfs.getImportedPools()
          |> List.ofSeq

      let zedPools =
        List.map (fun (x:Libzfs.Pool) ->
          Zpool.create x.name (Zpool.Guid x.uid) x.state (Some x.size)) libzfsPools

      let zedZfs =
        Seq.collect (fun (x:Libzfs.Pool) ->
          Seq.map (fun (y:Libzfs.Dataset) -> 
            let id = 
              libzfs.getDatasetStringProp(y.name, "guid")
                |> Option.expect (sprintf "could not fetch guid for %s" y.name)
                |> Zfs.Id

            Zfs.create (Zpool.Guid x.uid) y.name id
          ) x.datasets) libzfsPools

      {
        state with
          zpools = toMap (fun x -> x.guid) zedPools;
          zfs = toMap (fun x -> x.id) zedZfs;
          props = [];
      }
    | Zpool.Create x ->
      let size =
        (libzfs.getPoolByName x.name)
          |> Option.map (fun x -> x.size)

      let pool = { x with size = size }
      { state with zpools = Map.add pool.guid pool state.zpools }
    | Zpool.Import x | Zpool.Export x ->
      { state with zpools = Map.add x.guid x state.zpools }
    | Zpool.Destroy x ->
      { state with 
          zpools = Map.remove x.guid state.zpools;
          props = List.filter (Properties.byPoolGuid x.guid) state.props;
          zfs = Map.filter (fun _ z -> z.poolGuid <> x.guid) state.zfs;
      }
    | Zfs.Create x ->
      { state with zfs = Map.add x.id x state.zfs; }
    | Zfs.Destroy x ->
      let filterZfsProps (x:Zfs.Data) y =
        match y with
          | Properties.Zfs p ->
            p.poolGuid <> x.poolGuid && p.zfsId <> x.id
          | _  -> true

      { state with 
          zfs = Map.remove x.id state.zfs; 
          props = List.filter (filterZfsProps x) state.props; }
    | Properties.ZpoolProp (x:Properties.Property) ->
      { state with props = state.props @ [x] }
    | Properties.ZfsProp x ->
      { state with props = state.props @ [x] }
    | ZedGeneric ->
      state
    | x ->
      failwithf "Handler got a bad match %A" x

let handler = scan init update