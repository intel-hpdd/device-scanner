// Copyright (c) 2018 Intel Corporation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

module IML.DeviceScannerDaemon.Udev

open Fable.Core
open JsInterop
open IML
open Types.CommandTypes
open IML.CommonLibrary
open Thot.Json.Decode

[<Erase>]
type DevPath = DevPath of string
[<Erase>]
type Path = Path of string

let private splitSpace = String.split ' ' >> Array.map String.trim

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
        |> optional "DEVLINKS" (map (Option.map (splitSpace >> (Array.map Path))) (option string)) None
        |> required "DEVNAME" (map Path string)
        |> required "DEVPATH" (map DevPath string)
        |> required "DEVTYPE" string
        |> optional "ID_VENDOR" (option string) None
        |> optional "ID_MODEL" (option string) None
        |> optional "ID_SERIAL" (option string) None
        |> optional "ID_FS_TYPE" (map (Option.bind String.emptyStrToNone) (option string)) None
        |> optional "ID_FS_USAGE" (map (Option.bind String.emptyStrToNone) (option string)) None
        |> optional "ID_PART_ENTRY_NUMBER" (map (Option.map Operators.int) (option string)) None
        |> optional "IML_SIZE" (map (Option.bind String.emptyStrToNone) (option string)) None
        |> optional "IML_SCSI_80" (map (Option.map String.trim) (option string)) None
        |> optional "IML_SCSI_83" (map (Option.map String.trim) (option string)) None
        |> optional "IML_IS_RO" (map (Option.map isOne) (option string)) None
        |> optional "IML_DM_SLAVE_MMS" (map splitSpace string) [||]
        |> optional "IML_DM_VG_SIZE" (map (Option.map String.trim) (option string)) None
        |> custom (matchedKeyValuePairs (fun k -> String.startsWith "MD_DEVICE_" k && String.endsWith "_DEV" k) string)
        |> optional "DM_MULTIPATH_DEVICE_PATH" (map (Option.map isOne) (option string)) None
        |> optional "DM_LV_NAME" (option string) None
        |> optional "DM_VG_NAME" (option string) None
        |> optional "DM_UUID" (option string) None
        |> optional "MD_UUID" (option string) None

let uEventDecoder x =
  decodeString UEvent.Decoder x
    |> Result.mapError exn

type BlockDevices = Map<DevPath, UEvent>

let update (blockDevices:BlockDevices) (x:UdevCommand):Result<BlockDevices, exn> =
  match x with
    | Add o | Change o ->
      uEventDecoder o
        |> Result.map (fun d ->
          Map.add d.DEVPATH d blockDevices
        )
    | Remove o ->
      uEventDecoder o
        |> Result.map (fun d ->
          Map.remove d.DEVPATH blockDevices
        )
