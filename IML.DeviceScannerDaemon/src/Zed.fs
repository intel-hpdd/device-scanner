// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Zed

open Fable.Core
open IML.Types.CommandTypes
open CommonLibrary

[<RequireQualifiedAccess>]
module Zpool =
  /// Representation of a zpool from ZED events.
  type Data =
    {
      /// Name of the pool
      name: ZpoolName;
      /// Unique id of the imported pool.
      /// Exporting and importing the pool
      /// will change this guid.
      guid: Guid;
      hostName: string;
      /// The state of the pool.
      state: State;
      size: float;
      vdev: libzfs.Libzfs.VDev;
    }

  let create name guid hostName state size vdev =
    {
      name = name;
      guid = guid;
      hostName = hostName
      state = state;
      size = size;
      vdev = vdev
    }

[<RequireQualifiedAccess>]
module Zfs =
  type Data =
    {
      /// The pool the zfs item belongs to.
      poolGuid: Guid;
      /// The name of the zfs item
      name: ZfsName;
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
      poolGuid: Guid;
      /// The zfs name
      zfsName: ZfsName;
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
      poolGuid: Guid;
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

[<RequireQualifiedAccess>]
module Zed =
  open libzfs

  type ZedData = {
    zpools: Map<Guid, Zpool.Data>;
    zfs: Map<(Guid * ZfsName), Zfs.Data>;
    props: Set<Properties.Property>;
  }

  let update (zed:ZedData) (x:ZedCommand):ZedData =
    match x with
      | Init ->
        let libzfsPools = 
          libzfs.getImportedPools()
            |> List.ofSeq

        let zedPools =
          List.map (fun (x:Libzfs.Pool) ->
            Zpool.create (ZpoolName x.name) (Guid x.uid) x.hostname (State x.state) x.size x.vdev) libzfsPools

        let zedZfs =
          Seq.collect (fun (x:Libzfs.Pool) ->
            Seq.map (fun (y:Libzfs.Dataset) ->
              Zfs.create (Guid x.uid) (ZfsName y.name)
            ) x.datasets) libzfsPools

        {
          zed with
            zpools = toMap (fun x -> x.guid) zedPools;
            zfs = toMap (fun x -> (x.poolGuid, x.name)) zedZfs;
            props = Set.empty;
        }
      | CreateZpool (ZpoolName(name), guid, state) ->
        let pool = 
          libzfs.getPoolByName(name)
            |> Option.expect (sprintf "expected pool name %s to exist and be imported." name)
            |> fun p -> Zpool.create (ZpoolName name) guid p.hostname state p.size p.vdev

        {
          zed with
            zpools = Map.add guid pool zed.zpools;
        }
      | ImportZpool (guid, state) -> 
        let pool = Map.find guid zed.zpools
        
        {
          zed with
            zpools = Map.add guid ({pool with state = state}) zed.zpools;
        }
      | ExportZpool (guid, state) ->
        let pool = Map.find guid zed.zpools
        
        {
          zed with
            zpools = Map.add guid ({pool with state = state}) zed.zpools;
        }
      | DestroyZpool guid ->
        {
          zed with
            zpools = Map.remove guid zed.zpools;
            zfs = Map.filter (fun (guid', _) _ -> guid <> guid') zed.zfs
            props = Set.filter (Properties.byPoolGuid guid) zed.props
        }
      | CreateZfs (guid, zfsName) ->
        {
          zed with
            zfs = Map.add (guid, zfsName) { poolGuid = guid; name = zfsName; } zed.zfs
        }
      | DestroyZfs (guid, zfsName) ->
        {
          zed with
            zfs = Map.remove (guid, zfsName) zed.zfs
            props = Set.filter (function
              | Properties.Zfs z -> z.poolGuid <> guid && z.zfsName <> zfsName
              | _ -> true
            ) zed.props
        }
      | SetZpoolProp (guid, key, value) ->
        {
          zed with
            props = Set.add (Properties.createZpoolProperty guid key value) zed.props;
        }
      | SetZfsProp (guid, zfsName, key, value) -> 
        {
          zed with
            props = Set.add (Properties.createZfsProperty guid zfsName key value) zed.props;
        }
      | AddVdev guid ->
        let x = 
          maybe {
            let! zedPool = Map.tryFind guid zed.zpools

            let (ZpoolName name) = zedPool.name
            
            let! libzfsPool = libzfs.getPoolByName(name)

            return 
              { 
                zedPool with 
                  vdev = libzfsPool.vdev;
              }
          }
          |> Option.expect (sprintf "expected pool guid %A to exist and be imported." guid)

        {
          zed with
            zpools = Map.add x.guid x zed.zpools
        }