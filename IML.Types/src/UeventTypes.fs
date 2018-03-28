// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.Types.UeventTypes

open Fable.Core
open JsInterop
open IML.CommonLibrary
open Thot.Json

[<Erase>]
type DevPath = DevPath of string
[<Erase>]
type Path = Path of string

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
    vendor: string option;
    model: string option;
    serial: string option;
    fsType: string option;
    fsUsage: string option;
    partEntryNumber: int option;
    size: string option;
    scsi80: string option;
    scsi83: string option;
    readOnly: bool option;
    dmSlaveMMs: string [];
    dmVgSize: string option;
    mdDevs: string [];
    dmMultipathDevpath: bool option;
    dmLvName: string option;
    dmVgName: string option;
    dmUUID: string option;
    mdUUID: string option;
  }

module UEvent =
  let private stringProp x = Decode.required x Decode.string

  let private optionalString = Decode.option Decode.string
  let private optionalStringProp x = Decode.optional x optionalString None


  /// Decodes output from Udev
  /// Will not re-decode an entry
  /// encoded by UEvent
  let udevDecode =
    Decode.decode
        (fun major minor devlinks devname devpath devtype
             idVendor idModel idSerial idFsType idFsUsage
             idPartEntryNumber imlSize imlScsi80 imlScsi83
             imlIsRo imlDmSlaveMms imlDmVgSize imlMdDevices
             dmMultipathDevicePath dmLvName dmVgName dmUuid mdUuid ->

            { major = major
              minor = minor
              paths = Array.append devlinks [| devname |]
              devname = devname
              devpath = devpath
              devtype = devtype
              vendor = idVendor
              model = idModel
              serial = idSerial
              fsType = idFsType
              fsUsage = idFsUsage
              partEntryNumber = idPartEntryNumber
              size =  imlSize
              scsi80 = imlScsi80
              scsi83 = imlScsi83
              readOnly = imlIsRo
              dmSlaveMMs = imlDmSlaveMms
              dmVgSize = imlDmVgSize
              mdDevs = imlMdDevices |> List.map snd |> List.toArray
              dmMultipathDevpath = dmMultipathDevicePath
              dmLvName = dmLvName
              dmVgName = dmVgName
              dmUUID = dmUuid
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
        |> Decode.optional "ID_PART_ENTRY_NUMBER" (Decode.map (Option.map int) optionalString) None
        |> Decode.optional "IML_SIZE" (Decode.map (Option.bind String.emptyStrToNone) optionalString) None
        |> Decode.optional "IML_SCSI_80" (Decode.map (Option.map String.trim) optionalString) None
        |> Decode.optional "IML_SCSI_83" (Decode.map (Option.map String.trim) optionalString) None
        |> Decode.optional "IML_IS_RO" (Decode.map (Option.map isOne) optionalString) None
        |> Decode.optional "IML_DM_SLAVE_MMS" (Decode.map splitSpace Decode.string) [||]
        |> Decode.optional "IML_DM_VG_SIZE" (Decode.map (Option.map String.trim) optionalString) None
        |> Decode.custom (matchedKeyValuePairs (fun k -> String.startsWith "MD_DEVICE_" k && String.endsWith "_DEV" k) Decode.string)
        |> Decode.optional "DM_MULTIPATH_DEVICE_PATH" (Decode.map (Option.map isOne) optionalString) None
        |> optionalStringProp "DM_LV_NAME"
        |> optionalStringProp "DM_VG_NAME"
        |> optionalStringProp "DM_UUID"
        |> optionalStringProp "MD_UUID"

  /// Decodes output from Udev
  /// Will not re-decode an entry
  /// encoded by UEvent
  let udevDecoder x =
    Decode.decodeString udevDecode x
      |> Result.mapError exn

  let encoder
    {
      major = major
      minor = minor
      paths = paths
      devname = devName
      devpath = devPath
      devtype = devType
      vendor = idVendor
      model = idModel
      serial = idSerial
      fsType = idFsType
      fsUsage = idFsUsage
      partEntryNumber = idPartEntryNumber
      size =  imlSize
      scsi80 = imlScsi80
      scsi83 = imlScsi83
      readOnly = imlIsRo
      dmSlaveMMs = imlDmSlaveMms
      dmVgSize = imlDmVgSize
      mdDevs = imlMdDevices
      dmMultipathDevpath = dmMultipathDevicePath
      dmLvName = dmLvName
      dmVgName = dmVgName
      dmUUID = dmUuid
      mdUUID = mdUuid
    } =
      let pathValue (Path x) =
        Encode.string x

      let pathValues =
        paths
          |> Array.map pathValue

      let devPathValue (DevPath x) =
        Encode.string x

      let encodeStrings xs =
        Array.map Encode.string xs

      Encode.object [
        ("major", Encode.string major);
        ("minor", Encode.string minor);
        ("paths", Encode.array pathValues);
        ("devName", pathValue devName);
        ("devPath", devPathValue devPath);
        ("devType", Encode.string devType);
        ("idVendor", Encode.option Encode.string idVendor);
        ("idModel", Encode.option Encode.string idModel);
        ("idSerial", Encode.option Encode.string idSerial);
        ("idFsType", Encode.option Encode.string idFsType);
        ("idFsUsage", Encode.option Encode.string idFsUsage);
        ("idPartEntryNumber", Encode.option Encode.int idPartEntryNumber);
        ("size", Encode.option Encode.string imlSize);
        ("scsi80", Encode.option Encode.string imlScsi80);
        ("scsi83", Encode.option Encode.string imlScsi83);
        ("isReadOnly", Encode.option Encode.bool imlIsRo);
        ("dmSlaveMms", Encode.array (encodeStrings imlDmSlaveMms));
        ("dmVgSize", Encode.option Encode.string imlDmVgSize);
        ("mdDevices", Encode.array (encodeStrings imlMdDevices));
        ("dmMultipathDevicePath", Encode.option Encode.bool dmMultipathDevicePath);
        ("dmLvName", Encode.option Encode.string dmLvName);
        ("dmVgName", Encode.option Encode.string dmVgName);
        ("dmUuid", Encode.option Encode.string dmUuid);
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
