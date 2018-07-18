// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Zed

open IML.Types.CommandTypes
open IML.Types.ZedTypes
open IML.CommonLibrary
open libzfs

let private toMap key xs =
    let folder state x = Map.add (key x) x state
    Seq.fold folder Map.empty xs

module Libzfs =
    let getPool (Zpool.Name(name)) (Zpool.Guid(guid)) =
        let x = Hex.toUint64String guid
        getPoolByName (name)
        |> Option.filter (fun p -> p.guid = x)
        |> Option.toResult
               (exn
                    (sprintf
                         "expected pool with name %s and guid %s to be imported"
                         name x))

    let getDataset (Zfs.Name(name)) =
        getDatasetbyName name
        |> Option.toResult
               (exn
                    (sprintf "expected dataset with name %s to be imported" name))

[<RequireQualifiedAccess>]
module Zed =
    /// Given the state and a pool guid,
    /// Try to find the pool in the state.
    let getPoolInState state (Zpool.Guid(guid)) =
        let x = Hex.toUint64String guid
        Map.tryFind x state
        |> Option.toResult (exn (sprintf "Could not find pool with guid %s" x))

    /// Given the state and a pool
    /// Either add or replace the pool.
    let updatePool state (p : Libzfs.Pool) = Map.add p.guid p state

    /// Given a guid, remove the corresponding
    /// pool if it exists.
    let removePool state (Zpool.Guid(guid)) =
        Map.remove (Hex.toUint64String guid) state

    /// Given a pool and a dataset name, remove the
    /// corresponding dataset if it exists.
    let removeDataset (Zfs.Name name) (pool : Libzfs.Pool) : Libzfs.Pool =
        { pool with datasets =
                        pool.datasets |> Array.filter (fun d -> d.name <> name) }

    /// Given a list of props
    /// a key and a value,
    /// update or create the prop.
    let updateProp k (v : string) (xs : Libzfs.ZProp []) =
        xs
        |> Array.filter (fun p -> p.name <> k)
        |> Array.append [| { name = k
                             value = v } |]

    let update (state : Zed) (x : ZedCommand) : Result<Zed, exn> =
        match x with
        | Init ->
            let xs = getImportedPools()
            Ok(toMap (fun x -> x.guid) xs)
        | CreateZpool(name, guid, _) ->
            Libzfs.getPool name guid |> Result.map (updatePool state)
        | ImportZpool(name, guid, Zpool.State(s)) ->
            let (Zpool.Name n) = name
            guid
            |> getPoolInState state
            |> Result.map (fun p ->
                   { p with state = s
                            name = n })
            |> Result.bindError (fun _ -> Libzfs.getPool name guid)
            |> Result.map (updatePool state)
        | ExportZpool(guid, _) | DestroyZpool guid ->
            guid
            |> removePool state
            |> Ok
        | CreateZfs(guid, name) ->
            let (<*>) = Result.apply
            let (<!>) = Result.map
            let (Zfs.Name n) = name

            let fn (pool : Libzfs.Pool) (ds : Libzfs.Dataset) : Libzfs.Pool =
                { pool with datasets =
                                pool.datasets
                                |> Array.filter (fun d -> d.name <> n)
                                |> Array.append [| ds |] }
            (updatePool state)
            <!> (fn <!> (getPoolInState state guid) <*> (Libzfs.getDataset name))
        | DestroyZfs(guid, name) ->
            guid
            |> getPoolInState state
            |> Result.map ((removeDataset name) >> updatePool state)
        | SetZpoolProp(guid, key, value) ->
            guid
            |> getPoolInState state
            |> Result.map
                   (fun pool ->
                   { pool with props = updateProp key value pool.props })
            |> Result.map (updatePool state)
        | SetZfsProp(guid, zfsName, key, value) ->
            let updateDataset f (Zfs.Name(name)) (xs : Libzfs.Dataset []) =
                xs |> Array.map (fun x ->
                          if x.name = name then f x
                          else x)

            let updater (d : Libzfs.Dataset) =
                { d with props = updateProp key value d.props }
            guid
            |> getPoolInState state
            |> Result.map
                   (fun p ->
                   { p with datasets = updateDataset updater zfsName p.datasets })
            |> Result.map (updatePool state)
        | AddVdev guid ->
            guid
            |> getPoolInState state
            |> Result.bind (fun p -> Libzfs.getPool (Zpool.Name p.name) guid)
            |> Result.map (updatePool state)
