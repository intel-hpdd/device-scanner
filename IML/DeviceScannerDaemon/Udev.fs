// Copyright (c) 2017 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Udev

open Thot.Json.Decode
open Fable.Core
open JsInterop
open IML
open JsonDecoders
open StringUtils
open Fable.Import.Node.PowerPack.LineDelimitedJsonStream

[<Erase>]
type DevPath = DevPath of string
[<Erase>]
type Path = Path of string

let private splitSpace = split [|' '|] >> Array.map trim

let private isOne = function
  | "1" -> true
  | _ -> false

/// A hacky way to match arbitrary key / value pairs in an object.
/// This is being used because in the current approach the key names are dynamic.
/// It would be better if we can figure out a static representation of md devices.
let private matchedKeyValuePairs (fieldPred: string -> bool) (decoder : Decoder<'value>) (value: obj) : Result<(string * 'value) list, DecoderError> =
    if not (Helpers.isObject value) || Helpers.isArray value then
        BadPrimitive ("an object", value)
        |> Error
    else
        value
        |> Helpers.objectKeys
        |> List.filter (fieldPred)
        |> List.map (fun key -> (key, value?(key) |> unwrap decoder))
        |> Ok

/// The data emitted after processing a
/// udev block device add | change | remove event
type UEvent =
  {
    MAJOR: string;
    MINOR: string;
    PATHS: Path array option;
    DEVNAME: Path;
    DEVPATH: DevPath;
    DEVTYPE: string;
    ID_VENDOR: string option;
    ID_MODEL: string option;
    ID_SERIAL: string option;
    ID_FS_TYPE: string option;
    ID_FS_USAGE: string option;
    ID_PART_ENTRY_NUMBER: int option;
    IML_SIZE: string option;
    IML_SCSI_80: string option;
    IML_SCSI_83: string option;
    IML_IS_RO: bool option;
    IML_DM_SLAVE_MMS: string [];
    IML_DM_VG_SIZE: string option;
    IML_MD_DEVICES: string [];
    DM_MULTIPATH_DEVICE_PATH: bool option;
    DM_LV_NAME: string option;
    DM_VG_NAME: string option;
    DM_UUID: string option;
    MD_UUID: string option;
  }
  static member Decoder =
    decode
        (fun major minor devlinks devname devpath devtype
             idVendor idModel idSerial idFsType idFsUsage
             idPartEntryNumber imlSize imlScsi80 imlScsi83
             imlIsRo imlDmSlaveMms imlDmVgSize imlMdDevices
             dmMultipathDevicePath dmLvName dmVgName dmUuid mdUuid ->

            { MAJOR = major
              MINOR = minor
              PATHS = Option.map (Array.append [| devname |]) devlinks
              DEVNAME = devname
              DEVPATH = devpath
              DEVTYPE = devtype
              ID_VENDOR = idVendor
              ID_MODEL = idModel
              ID_SERIAL = idSerial
              ID_FS_TYPE = idFsType
              ID_FS_USAGE = idFsUsage
              ID_PART_ENTRY_NUMBER = idPartEntryNumber
              IML_SIZE =  imlSize
              IML_SCSI_80 = imlScsi80
              IML_SCSI_83 = imlScsi83
              IML_IS_RO = imlIsRo
              IML_DM_SLAVE_MMS = imlDmSlaveMms
              IML_DM_VG_SIZE = imlDmVgSize
              IML_MD_DEVICES = imlMdDevices |> List.map snd |> List.toArray
              DM_MULTIPATH_DEVICE_PATH = dmMultipathDevicePath
              DM_LV_NAME = dmLvName
              DM_VG_NAME = dmVgName
              DM_UUID = dmUuid
              MD_UUID = mdUuid
              } : UEvent)
        |> required "MAJOR" string
        |> required "MINOR" string
        |> required "DEVLINKS" (andThenSucceed (Option.map (splitSpace >> (Array.map Path))) (option string))
        |> required "DEVNAME" (andThenSucceed Path string)
        |> required "DEVPATH" (andThenSucceed DevPath string)
        |> required "DEVTYPE" string
        |> required "ID_VENDOR" (option string)
        |> required "ID_MODEL" (option string)
        |> required "ID_SERIAL" (option string)
        |> required "ID_FS_TYPE" (andThenSucceed (Option.bind emptyStrToNone) (option string))
        |> required "ID_FS_USAGE" (andThenSucceed (Option.bind emptyStrToNone) (option string))
        |> required "ID_PART_ENTRY_NUMBER" (andThenSucceed (Option.map Operators.int) (option string))
        |> required "IML_SIZE" (andThenSucceed (Option.bind emptyStrToNone) (option string))
        |> required "IML_SCSI_80" (andThenSucceed (Option.map trim) (option string))
        |> required "IML_SCSI_83" (andThenSucceed (Option.map trim) (option string))
        |> required "IML_IS_RO" (andThenSucceed (Option.map isOne) (option string))
        |> optional "IML_DM_SLAVE_MMS" (andThenSucceed splitSpace string) [||]
        |> required "IML_DM_VG_SIZE" (andThenSucceed (Option.map trim) (option string))
        |> custom (matchedKeyValuePairs (fun k -> startsWith "MD_DEVICE_" k && endsWith "_DEV" k) string)
        |> required "DM_MULTIPATH_DEVICE_PATH" (andThenSucceed (Option.map isOne) (option string))
        |> required "DM_LV_NAME" (option string)
        |> required "DM_VG_NAME" (option string)
        |> required "DM_UUID" (option string)
        |> required "MD_UUID" (option string)

let actionDecoder = decodeJson (field "ACTION" string)

let uEventDecoder x =
  match decodeJson UEvent.Decoder x with
    | Ok y -> y
    | Error y -> failwith y

let (|UdevAdd|_|) (x:Json) =
  match actionDecoder x with
    | Ok(y) when y = "add" -> Some(uEventDecoder x)
    | _ -> None

let (|UdevChange|_|) (x:Json) =
  match actionDecoder x with
    | Ok(y) when y = "change" -> Some (uEventDecoder x)
    | _ -> None

let (|UdevRemove|_|) (x:Json) =
  match actionDecoder x with
    | Ok(y) when y = "remove" -> Some (uEventDecoder x)
    | _ -> None
