// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Zed

open Fable.Core
open IML.Types.CommandTypes
open CommonLibrary
open libzfs

[<RequireQualifiedAccess>]
module Zpool =
  /// Representation of a zpool from ZED events.
  type Data =
    {
      /// Name of the pool
      name: Zpool.Name;
      /// Unique id of the imported pool.
      /// Exporting and importing the pool
      /// will change this guid.
      guid: Zpool.Guid;
      hostName: string;
      hostId: float option;
      /// The state of the pool.
      state: Zpool.State;
      /// The size of the pool.
      size: float;
      /// The Vdev tree of the pool.
      vdev: Libzfs.VDev;
    }

  let create name guid hostName hostId state size vdev =
    {
      name = name;
      guid = guid;
      hostName = hostName;
      hostId = hostId;
      state = state;
      size = size;
      vdev = vdev
    }

[<RequireQualifiedAccess>]
module Zfs =
  type Data =
    {
      /// The pool the zfs item belongs to.
      poolGuid: Zpool.Guid;
      /// The name of the zfs item
      name: Zfs.Name;
    }

  let create poolGuid name =
    {
      poolGuid = poolGuid;
      name = name;
    }

[<RequireQualifiedAccess>]
module Properties =
  /// Representation of a ZFS property.
  /// This is a nvpair in ZFS.
  type ZfsProperty =
    {
      /// The guid of the pool this property is associated with.
      poolGuid: Zpool.Guid;
      /// The zfs name
      zfsName: Zfs.Name;
      /// The property name
      key: string;
      /// The property value
      value: string;
    }

  /// Representation of a zpool property.
  /// This is a nvpair in ZFS.
  type ZpoolProperty =
    {
      /// The guid of the pool this property is associated with.
      poolGuid: Zpool.Guid;
      /// The property name
      key: string;
      /// The property value
      value: string;
    }

  [<Erase>]
  type Property =
    | Zpool of ZpoolProperty
    | Zfs of ZfsProperty

  let createZpoolProperty poolGuid key value =
    {
      poolGuid = poolGuid;
      key = key;
      value = value;
    }
      |> Zpool

  let createZfsProperty poolGuid zfsName key value =
      {
        poolGuid = poolGuid;
        /// The zfs name
        zfsName = zfsName;
        /// The property name
        key = key;
        /// The property value
        value = value;
      }
        |> Zfs

  let byPoolGuid x y =
        match y with
          | Zfs p -> p.poolGuid <> x
          | Zpool p -> p.poolGuid <> x


let private toMap key xs =
  let folder state x =
    Map.add (key x) x state

  Seq.fold folder Map.empty xs

module Libzfs =
  let getImportedPools () =
    libzfs.Invoke().getImportedPools()

  let libzfsPooltoZedPoolDatasets (x:Libzfs.Pool) =
    let pool = Zpool.create (Zpool.Name x.name) (Zpool.Guid x.uid) x.hostname x.hostid (Zpool.State x.state) x.size x.vdev

    let ds =
      x.datasets
        |> Seq.map (fun (y:Libzfs.Dataset) ->
          Zfs.create (Zpool.Guid x.uid) (Zfs.Name y.name)
        )
        |> Set.ofSeq

    pool, ds

  let getPoolByName (n:string):Result<Zpool.Data * Set<Zfs.Data>, exn> =
    libzfs.Invoke().getPoolByName n
      |> Option.map (libzfsPooltoZedPoolDatasets)
      |> Option.toResult (exn (sprintf "expected pool name %s to exist and be imported." n))

  let tryFindPool (Zpool.Guid guid) =
      getImportedPools()
        |> List.tryFind(fun p -> p.uid = guid)
        |> Option.map (libzfsPooltoZedPoolDatasets)
        |> Option.toResult (exn (sprintf "expected pool guid %s to exist and be imported." guid))

[<RequireQualifiedAccess>]
module Zed =
  type ZedData = {
    zpools: Map<Zpool.Guid, Zpool.Data>;
    zfs: Set<Zfs.Data>;
    props: Set<Properties.Property>;
  }

  let update (zed:ZedData) (x:ZedCommand):Result<ZedData, exn> =
    match x with
      | Init ->
        let (pools, zfses) =
          Libzfs.getImportedPools()
            |> List.map (Libzfs.libzfsPooltoZedPoolDatasets)
            |> List.unzip

        Ok({
            zed with
              zpools = toMap (fun x -> x.guid) pools;
              zfs =  Set.unionMany zfses;
              props = Set.empty;
        })
      | CreateZpool (Zpool.Name(name), guid, state) ->
          Libzfs.getPoolByName(name)
            |> Result.map (fun (p, _) ->
              let p' = {
                p with
                  guid = guid;
                  state = state;
              }

              {
                zed with
                  zpools = Map.add guid p' zed.zpools;
              }
            )
      | ImportZpool (guid, state) ->
            match Map.tryFind guid zed.zpools with
              | Some p ->
                Ok({
                    zed with
                      zpools = Map.add guid ({p with state = state}) zed.zpools;
                  })
              | None ->
                 Libzfs.tryFindPool guid
                  |> Result.map (fun (p, ds) ->
                    {
                      zed with
                        zpools = Map.add guid p zed.zpools
                        zfs = Set.union zed.zfs ds
                    }
                  )
      | ExportZpool (guid, state) ->
        let pool = Map.find guid zed.zpools

        Ok({
            zed with
              zpools = Map.add guid ({pool with state = state}) zed.zpools;
        })
      | DestroyZpool guid ->
        Ok({
            zed with
              zpools = Map.remove guid zed.zpools;
              zfs = Set.filter (fun (x) -> guid <> x.poolGuid) zed.zfs
              props = Set.filter (Properties.byPoolGuid guid) zed.props
        })
      | CreateZfs (guid, zfsName) ->
        Ok({
            zed with
              zfs = Set.add { poolGuid = guid; name = zfsName; } zed.zfs
        })
      | DestroyZfs (guid, zfsName) ->
        Ok({
            zed with
              zfs = Set.filter (fun (x) -> guid <> x.poolGuid) zed.zfs
              props = Set.filter (function
                | Properties.Zfs z -> z.poolGuid <> guid && z.zfsName <> zfsName
                | _ -> true
              ) zed.props
        })
      | SetZpoolProp (guid, key, value) ->
        Ok({
            zed with
              props = Set.add (Properties.createZpoolProperty guid key value) zed.props;
        })
      | SetZfsProp (guid, zfsName, key, value) ->
        Ok({
            zed with
              props = Set.add (Properties.createZfsProperty guid zfsName key value) zed.props;
        })
      | AddVdev guid ->
        let ex = exn (sprintf "expected pool guid %A to exist and be imported." guid)

        Map.tryFind guid zed.zpools
          |> Option.toResult ex
          |> Result.bind(fun pool ->
            let (Zpool.Name name) = pool.name

            Libzfs.getPoolByName name
          )
          |> Result.map (fun (p, _) ->
            {
              zed with
                zpools = Map.add p.guid p zed.zpools
            }
          )
