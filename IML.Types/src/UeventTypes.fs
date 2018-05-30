// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.UeventTypes

open System.Text.RegularExpressions

open Fable.Import.Node

open Fable.Core
open JsInterop

open Thoth.Json

open IML.CommonLibrary


let private splitSpace (x:string) =  x.Split([| ' ' |], System.StringSplitOptions.RemoveEmptyEntries)
let private isOne x = x = "1"

/// A hacky way to match arbitrary key / value pairs in an object.
/// This is being used because in the current approach the key names are dynamic.
/// It would be better if we can figure out a static representation of md devices.
let private matchedKeyValuePairs (fieldPred: string -> bool) (decoder : Decode.Decoder<'value>) (value: obj) : Result<(string * 'value) list, Decode.DecoderError> =
    if not (Decode.Helpers.isObject value) || Decode.Helpers.isArray value then
        Decode.BadPrimitive ("an object", value)
        |> Error
    else
        value
        |> Decode.Helpers.objectKeys
        |> List.filter (fieldPred)
        |> List.map (fun key -> (key, value?(key) |> Decode.unwrap decoder))
        |> Ok

let decodeLvmUuids (value:obj) =
  let lvmPfix = "LVM-"
  let uuidLen = 32

  if Decode.Helpers.isString value then
    let dmUuid:string = unbox value

    if not (dmUuid.StartsWith lvmPfix) then
      Decode.FailMessage (sprintf "%s does not appear to be dmUuid" dmUuid)
        |> Error
    else
      let uuids = dmUuid.[lvmPfix.Length..]

      if uuids.Length <> (uuidLen * 2) then
        Decode.FailMessage (sprintf "%s does not have the expected length" dmUuid)
          |> Error
      else
        (uuids.[0..(uuidLen - 1)], uuids.[uuidLen..])
          |> Ok
  else
    Decode.BadPrimitive ("a string", value)
      |> Error

[<Erase>]
type DevPath = DevPath of string

[<Erase>]
type Path = Path of string

/// The data emitted after processing a
/// udev block device add | change | remove event
[<Pojo>]
type UEvent =
  {
    major: string;
    minor: string;
    paths: Path array;
    devname: Path;
    devpath: DevPath;
    devtype: string;
    parent: DevPath option;
    vendor: string option;
    model: string option;
    serial: string option;
    fsType: string option;
    fsUsage: string option;
    fsUuid: string option;
    partEntryNumber: int option;
    size: string option;
    scsi80: string option;
    scsi83: string option;
    readOnly: bool option;
    biosBoot: bool;
    zfsReserved: bool;
    isMpath: bool;
    dmSlaveMMs: string [];
    dmVgSize: string option;
    mdDevs: string [];
    dmMultipathDevpath: bool option;
    dmName: string option;
    dmLvName: string option;
    lvUuid: string option;
    dmVgName: string option;
    vgUuid: string option;
    mdUUID: string option;
  }

module UEvent =
  let private stringProp x = Decode.required x Decode.string
  let private stringPropOption x = Decode.required x (Decode.option Decode.string)

  let private optionalString = Decode.option Decode.string
  let private optionalStringProp x = Decode.optional x optionalString None

  let private convertSize x =
    x
      |> Option.map int
      |> Option.map ((*) 512)
      |> Option.map string

  let pathValue (Path x) =
    Encode.string x

  let pathValues xs =
    Array.map pathValue xs

  let devPathValue (DevPath x) =
    Encode.string x

  let majorMinor x =
    sprintf "%s:%s" x.major x.minor

  let devPathRegex = "^/dev/[^/]+$"
  let diskByIdRegex = "^/dev/disk/by-id/"
  let diskByPathRegex = "^/dev/disk/by-path/"
  let mapperPathRegex = "^/dev/mapper/"

  let private precedence = [|
    mapperPathRegex;
    diskByIdRegex;
    diskByPathRegex;
    ".+";
  |]

  let private idx (Path(x)) =
    Array.findIndex (fun p ->
      Regex.Match(x, p).Success
    ) precedence

  let private sortPaths =
    Array.sortBy idx

  /// Decodes output from Udev
  /// Will not re-decode an entry
  /// encoded by UEvent
  let udevDecode =
    Decode.decode
        (fun major minor devlinks devname devpath devtype
             idVendor idModel idSerial idFsType idFsUsage idFsUuid
             idPartEntryNumber imlSize imlScsi80 imlScsi83
             imlIsRo imlIsBiosBoot imlIsZfsReserved imlIsMpath imlDmSlaveMms
             imlDmVgSize imlMdDevices dmMultipathDevicePath dmName dmLvName
             dmVgName dmUuid mdUuid ->

            { major = major
              minor = minor
              paths =
                [| devname |]
                  |> Array.append devlinks
                  |> sortPaths
              devname = devname
              devpath = devpath
              devtype = devtype
              parent = None
              vendor = idVendor
              model = idModel
              serial = idSerial
              fsType = idFsType
              fsUsage = idFsUsage
              fsUuid = idFsUuid
              partEntryNumber = idPartEntryNumber
              size =  imlSize
              scsi80 = imlScsi80
              scsi83 = imlScsi83
              readOnly = imlIsRo
              biosBoot = imlIsBiosBoot
              zfsReserved = imlIsZfsReserved
              isMpath = imlIsMpath
              dmSlaveMMs = imlDmSlaveMms
              dmVgSize = imlDmVgSize
              mdDevs = imlMdDevices |> List.map snd |> List.toArray
              dmMultipathDevpath = dmMultipathDevicePath
              dmName = dmName
              dmLvName = dmLvName
              lvUuid = Option.map snd dmUuid
              dmVgName = dmVgName
              vgUuid = Option.map fst dmUuid
              mdUUID = mdUuid
              } : UEvent)
        |> stringProp "MAJOR"
        |> stringProp "MINOR"
        |> Decode.optional "DEVLINKS" (Decode.map (splitSpace >> (Array.map Path)) Decode.string) [| |]
        |> Decode.required "DEVNAME" (Decode.map Path Decode.string)
        |> Decode.required "DEVPATH" (Decode.map DevPath Decode.string)
        |> Decode.required "DEVTYPE" Decode.string
        |> optionalStringProp "ID_VENDOR"
        |> optionalStringProp "ID_MODEL"
        |> optionalStringProp "ID_SERIAL"
        |> Decode.optional "ID_FS_TYPE" (Decode.map (Option.bind String.emptyStrToNone) optionalString) None
        |> Decode.optional "ID_FS_USAGE" (Decode.map (Option.bind String.emptyStrToNone) optionalString) None
        |> Decode.optional "ID_FS_UUID" (Decode.map (Option.bind String.emptyStrToNone) optionalString) None
        |> Decode.optional "ID_PART_ENTRY_NUMBER" (Decode.map (Option.map int) optionalString) None
        |> Decode.optional "IML_SIZE" (Decode.map (Option.bind (String.emptyStrToNone >> convertSize)) optionalString) None
        |> Decode.optional "IML_SCSI_80" (Decode.map (Option.map String.trim) optionalString) None
        |> Decode.optional "IML_SCSI_83" (Decode.map (Option.map String.trim) optionalString) None
        |> Decode.optional "IML_IS_RO" (Decode.map (Option.map isOne) optionalString) None
        |> Decode.optional "IML_IS_BIOS_BOOT" (Decode.map isOne Decode.string) false
        |> Decode.optional "IML_IS_ZFS_RESERVED" (Decode.map isOne Decode.string) false
        |> Decode.optional "IML_IS_MPATH" (Decode.map isOne Decode.string) false
        |> Decode.optional "IML_DM_SLAVE_MMS" (Decode.map splitSpace Decode.string) [||]
        |> Decode.optional "IML_DM_VG_SIZE" optionalString None
        |> Decode.custom (matchedKeyValuePairs (fun k -> String.startsWith "MD_DEVICE_" k && String.endsWith "_DEV" k) Decode.string)
        |> Decode.optional "DM_MULTIPATH_DEVICE_PATH" (Decode.map (Option.map isOne) optionalString) None
        |> optionalStringProp "DM_NAME"
        |> optionalStringProp "DM_LV_NAME"
        |> optionalStringProp "DM_VG_NAME"
        |> Decode.optional "DM_UUID" (Decode.option decodeLvmUuids) None
        |> optionalStringProp "MD_UUID"

  /// Decodes output from Udev
  /// Will not re-decode an entry
  /// encoded by UEvent
  let udevDecoder x =
    Decode.decodeString udevDecode x
      |> Result.mapError exn

  let encodedDecode =
    Decode.decode
      (fun major minor paths devname devpath devtype parent
           vendor model serial fsType fsUsage fsUuid
           partEntryNumber size scsi80 scsi83
           readOnly biosBoot zfsReserved isMpath dmSlaveMMs dmVgSize mdDevs
           dmMultipathDevicePath dmName dmLvName lvUuid dmVgName vgUuid mdUuid ->

          { major = major
            minor = minor
            paths = paths
            devname = devname
            devpath = devpath
            devtype = devtype
            parent = parent
            vendor = vendor
            model = model
            serial = serial
            fsType = fsType
            fsUsage = fsUsage
            fsUuid = fsUuid
            partEntryNumber = partEntryNumber
            size =  size
            scsi80 = scsi80
            scsi83 = scsi83
            readOnly = readOnly
            biosBoot = biosBoot
            zfsReserved = zfsReserved
            isMpath = isMpath
            dmSlaveMMs = dmSlaveMMs
            dmVgSize = dmVgSize
            mdDevs = mdDevs
            dmMultipathDevpath = dmMultipathDevicePath
            dmName = dmName
            dmLvName = dmLvName
            lvUuid = lvUuid
            dmVgName = dmVgName
            vgUuid = vgUuid
            mdUUID = mdUuid
            } : UEvent)
        |> stringProp "major"
        |> stringProp "minor"
        |> Decode.required "paths" (Decode.array (Decode.map Path Decode.string))
        |> Decode.required "devName" (Decode.map Path Decode.string)
        |> Decode.required "devPath" (Decode.map DevPath Decode.string)
        |> stringProp "devType"
        |> Decode.required "parent" (Decode.map (Option.map DevPath) (Decode.option Decode.string))
        |> stringPropOption "idVendor"
        |> stringPropOption "idModel"
        |> stringPropOption "idSerial"
        |> stringPropOption "idFsType"
        |> stringPropOption "idFsUsage"
        |> stringPropOption "idFsUuid"
        |> Decode.required "idPartEntryNumber" (Decode.option Decode.int)
        |> stringPropOption "size"
        |> stringPropOption "scsi80"
        |> stringPropOption "scsi83"
        |> Decode.required "isReadOnly" (Decode.option Decode.bool)
        |> Decode.required "isBiosBoot" Decode.bool
        |> Decode.required "isZfsReserved" Decode.bool
        |> Decode.required "isMpath" Decode.bool
        |> Decode.required "dmSlaveMms" (Decode.array Decode.string)
        |> stringPropOption "dmVgSize"
        |> Decode.required "mdDevices" (Decode.array Decode.string)
        |> Decode.required "dmMultipathDevicePath" (Decode.option Decode.bool)
        |> stringPropOption "dmName"
        |> stringPropOption "dmLvName"
        |> stringPropOption "lvUuid"
        |> stringPropOption "dmVgName"
        |> stringPropOption "vgUuid"
        |> stringPropOption "mdUuid"

  let encodedDecoder x =
    Decode.decodeString encodedDecode x
      |> Result.mapError exn

  let encoder
    {
      major = major
      minor = minor
      paths = paths
      devname = devName
      devpath = devPath
      parent = parent
      devtype = devType
      vendor = idVendor
      model = idModel
      serial = idSerial
      fsType = idFsType
      fsUsage = idFsUsage
      fsUuid = idFsUuid
      partEntryNumber = idPartEntryNumber
      size =  imlSize
      scsi80 = imlScsi80
      scsi83 = imlScsi83
      readOnly = imlIsRo
      biosBoot = imlIsBiosBoot
      zfsReserved = imlIsZfsReserved
      isMpath = imlIsMpath
      dmSlaveMMs = imlDmSlaveMms
      dmVgSize = imlDmVgSize
      mdDevs = imlMdDevices
      dmMultipathDevpath = dmMultipathDevicePath
      dmName = dmName
      dmLvName = dmLvName
      lvUuid = lvUuid
      dmVgName = dmVgName
      vgUuid = vgUuid
      mdUUID = mdUuid
    } =
      Encode.object [
        ("major", Encode.string major);
        ("minor", Encode.string minor);
        ("paths", Encode.array (pathValues paths));
        ("devName", pathValue devName);
        ("devPath", devPathValue devPath);
        ("parent", Encode.option devPathValue parent);
        ("devType", Encode.string devType);
        ("idVendor", Encode.option Encode.string idVendor);
        ("idModel", Encode.option Encode.string idModel);
        ("idSerial", Encode.option Encode.string idSerial);
        ("idFsType", Encode.option Encode.string idFsType);
        ("idFsUsage", Encode.option Encode.string idFsUsage);
        ("idFsUuid", Encode.option Encode.string idFsUuid);
        ("idPartEntryNumber", Encode.option Encode.int idPartEntryNumber);
        ("size", Encode.option Encode.string imlSize);
        ("scsi80", Encode.option Encode.string imlScsi80);
        ("scsi83", Encode.option Encode.string imlScsi83);
        ("isReadOnly", Encode.option Encode.bool imlIsRo);
        ("isBiosBoot", Encode.bool imlIsBiosBoot);
        ("isZfsReserved", Encode.bool imlIsZfsReserved);
        ("isMpath", Encode.bool imlIsMpath);
        ("dmSlaveMms", Encode.array (encodeStrings imlDmSlaveMms));
        ("dmVgSize", Encode.option Encode.string imlDmVgSize);
        ("mdDevices", Encode.array (encodeStrings imlMdDevices));
        ("dmMultipathDevicePath", Encode.option Encode.bool dmMultipathDevicePath);
        ("dmName", Encode.option Encode.string dmName);
        ("dmLvName", Encode.option Encode.string dmLvName);
        ("lvUuid", Encode.option Encode.string lvUuid);
        ("dmVgName", Encode.option Encode.string dmVgName);
        ("vgUuid", Encode.option Encode.string vgUuid);
        ("mdUuid", Encode.option Encode.string mdUuid);
      ]

type BlockDevices = Map<DevPath, UEvent>

module BlockDevices =
  let encoder x =
    x
      |> Map.toList
      |> List.map
          (fun (DevPath x, y) ->
            (x, UEvent.encoder y)
          )
      |> Encode.object

  let decoder x =
    x
      |> Decode.dict UEvent.encodedDecode
      |> Result.map (fun x ->
          x
            |> Map.toList
            |> List.map (fun (k, v) ->
              (DevPath k, v)
            )
            |> Map.ofList
      )

  let linkParents x =
    let disks =
      x
        |> Map.filter (fun _ x -> x.devtype = "disk")

    x
      |> Map.map (fun (DevPath k) x ->
        let parent =
          path.dirname k
            |> DevPath
            |> fun v -> Map.tryFind v disks
            |> Option.map (fun v -> v.devpath)

        {
          x with
            parent = parent
        }
      )

  let tryFindByPath blockDevices x =
    blockDevices
      |> Map.tryFindKey(fun _ b ->
        Array.contains x b.paths
      )
      |> Option.map (fun y -> Map.find y blockDevices)

  let tryFindByMajorMinor blockDevices x =
    blockDevices
      |> Map.tryFindKey (fun _ b ->
        UEvent.majorMinor b = x
      )
      |> Option.map (fun y -> Map.find y blockDevices)
