module IML.DeviceScannerDaemon.EventTypesTest

open IML.DeviceScannerDaemon.TestFixtures
open IML.DeviceScannerDaemon.EventTypes
open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open Fable.PowerPack

let toJson =  Json.ofString >> Result.unwrapResult

let createAddEventJson = createEventJson addObj

let addDiskObj = createAddEventJson (fun x ->
  x
    |> Map.add "DEVTYPE" (Json.String("disk")))

let addDmDiskObj = createAddEventJson (fun x ->
  x
    |> Map.add "DEVTYPE" (Json.String("disk"))
    |> Map.add "DM_UUID" (Json.String("LVM-ABCD-1234-ABCD-1234"))
    |> Map.add "DM_SLAVE_MMS" (Json.String("252:1")))

let addInvalidDevTypeObj = createAddEventJson (fun x ->
  x
    |> Map.add "DEVTYPE" (Json.String("invalid")))

let missingDevNameObj = createAddEventJson (fun x ->
  x
    |> Map.remove "DEVNAME")

let floatDevTypeObj = createAddEventJson (fun x ->
  x
    |> Map.add "DEVTYPE" (Json.Number(7.0)))

let addMatch = function
  | AddOrChangeEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

let infoMatch = function
  | InfoEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

let removeMatch = function
  | RemoveEventMatch x -> x
  | _ -> raise (System.Exception "No Match")

test "Matching Events" <| fun () ->
  expect.assertions 9

  expect.Invoke(addMatch addObj).toMatchSnapshot()

  expect.Invoke(addMatch addDiskObj).toMatchSnapshot()

  expect.Invoke(addMatch addDmDiskObj).toMatchSnapshot()

  expect.Invoke(removeMatch removeObj).toMatchSnapshot()
  //matcher removeObj === "removeEvent"

  expect.Invoke(infoMatch (toJson """{ "ACTION": "info" }""")).toMatchSnapshot()

  try
    addMatch (toJson """{ "ACTION": "blah" }""") |> ignore
  with
    | msg ->
      msg.Message === "No Match"

  try
    addMatch addInvalidDevTypeObj |> ignore
  with
    | msg ->
      msg.Message === "DEVTYPE neither partition or disk"

  try
    addMatch missingDevNameObj |> ignore
  with
    | msg ->
      msg.Message === "Could not find key DEVNAME in map [[\"ACTION\",{\"tag\":0,\"data\":\"add\"}]; [\"DEVLINKS\",{\"tag\":0,\"data\":\"/dev/disk/by-id/ata-VBOX_HARDDISK_VB304a0a0f-15e93f07-part1 /dev/disk/by-path/pci-0000:00:01.1-ata-1.0-part1\"}]; [\"DEVPATH\",{\"tag\":0,\"data\":\"/devices/pci0000:00/0000:00:01.1/ata1/host1/target1:0:0/1:0:0:0/block/sdb/sdb1\"}]; [\"DEVTYPE\",{\"tag\":0,\"data\":\"partition\"}]; [\"DM_LV_NAME\",{\"tag\":0,\"data\":\"swap\"}]; [\"DM_MULTIPATH_DEVICE_PATH\",{\"tag\":0,\"data\":\"1\"}]; [\"DM_SLAVE_MMS\",{\"tag\":0,\"data\":\"252:2\"}]; [\"DM_UUID\",{\"tag\":0,\"data\":\"LVM-pV8TgNKMJVNrolJgMhVwg4CAeFFAIMC83IU1hvimWWlvmd5xQddtMIqRtjwOuKTz\"}]; [\"DM_VG_NAME\",{\"tag\":0,\"data\":\"centos\"}]; [\"DM_VG_SIZE\",{\"tag\":0,\"data\":\" 20946354176B\"}]; [\"ID_ATA\",{\"tag\":0,\"data\":\"1\"}]; [\"ID_ATA_FEATURE_SET_PM\",{\"tag\":1,\"data\":1}]; [\"ID_ATA_FEATURE_SET_PM_ENABLED\",{\"tag\":1,\"data\":1}]; [\"ID_ATA_WRITE_CACHE\",{\"tag\":1,\"data\":1}]; [\"ID_ATA_WRITE_CACHE_ENABLED\",{\"tag\":1,\"data\":1}]; [\"ID_BUS\",{\"tag\":0,\"data\":\"ata\"}]; [\"ID_FS_TYPE\",{\"tag\":0,\"data\":\"LVM2_member\"}]; [\"ID_MODEL\",{\"tag\":0,\"data\":\"VBOX_HARDDISK\"}]; [\"ID_MODEL_ENC\",{\"tag\":0,\"data\":\"VBOX HARDDISK\"}]; [\"ID_PART_ENTRY_DISK\",{\"tag\":0,\"data\":\"8:16\"}]; [\"ID_PART_ENTRY_NUMBER\",{\"tag\":0,\"data\":\"1\"}]; [\"ID_PART_ENTRY_OFFSET\",{\"tag\":0,\"data\":\"2048\"}]; [\"ID_PART_ENTRY_SCHEME\",{\"tag\":0,\"data\":\"dos\"}]; [\"ID_PART_ENTRY_SIZE\",{\"tag\":0,\"data\":\"2048\"}]; [\"ID_PART_ENTRY_TYPE\",{\"tag\":0,\"data\":\"0x83\"}]; [\"ID_PART_TABLE_TYPE\",{\"tag\":0,\"data\":\"dos\"}]; [\"ID_PATH\",{\"tag\":0,\"data\":\"pci-0000:00:01.1-ata-1.0\"}]; [\"ID_PATH_TAG\",{\"tag\":0,\"data\":\"pci-0000_00_01_1-ata-1_0\"}]; [\"ID_REVISION\",{\"tag\":0,\"data\":\"1.0\"}]; [\"ID_SERIAL\",{\"tag\":0,\"data\":\"VBOX_HARDDISK_VB304a0a0f-15e93f07\"}]; [\"ID_SERIAL_SHORT\",{\"tag\":0,\"data\":\"VB304a0a0f-15e93f07\"}]; [\"ID_TYPE\",{\"tag\":0,\"data\":\"disk\"}]; [\"IML_IS_RO\",{\"tag\":0,\"data\":\"1\"}]; [\"IML_SCSI_80\",{\"tag\":0,\"data\":\"80\"}]; [\"IML_SCSI_83\",{\"tag\":0,\"data\":\"83\"}]; [\"IML_SIZE\",{\"tag\":0,\"data\":\"81784832\"}]; [\"MAJOR\",{\"tag\":0,\"data\":\"8\"}]; [\"MINOR\",{\"tag\":0,\"data\":\"17\"}]; [\"SEQNUM\",{\"tag\":0,\"data\":\"1566\"}]; [\"SUBSYSTEM\",{\"tag\":0,\"data\":\"block\"}]; [\"TAGS\",{\"tag\":0,\"data\":\":systemd:\"}]; [\"USEC_INITIALIZED\",{\"tag\":0,\"data\":\"842\"}]]"

  try
    addMatch floatDevTypeObj |> ignore
  with
    msg ->
      msg.Message === "Invalid JSON, it must be a string"
