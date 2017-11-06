module IML.DeviceScannerDaemon.IntegrationTest

open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open NodeHelpers
open IML.Test.VagrantTest
open Fable.PowerPack


type CommandFn = unit -> Fable.Import.JS.Promise<Result<ExecOk, ExecErr>>

type Command =
  | Setup of CommandFn
  | SetupTeardown of (CommandFn * CommandFn)

type CommandBuilder() =
  let mutable stack = []
  let runTeardowns () =
      let start = Promise.lift(Ok(Stdout(""), Stderr("")))

      let result = List.fold (fun acc fn -> acc |> Promise.bind(fun _ -> fn())) start stack

      stack <- []

      result
  member _this.Bind(command, f) =
    let runSetup (x:CommandFn) =
      x()
        |> Promise.bind (function
          | Error (e, _, _) ->
            runTeardowns()
              |> Promise.map(fun _ -> (raise (e :?> System.Exception)))
          | x -> f x
          )

    match command with
      | Setup(fn) -> runSetup fn
      | SetupTeardown(setup, teardown) ->
        stack <- List.append [teardown] stack
        runSetup setup

  member _this.Return _ =
    runTeardowns()

  member _this.Zero() =
    runTeardowns()

let commandBuilder = CommandBuilder()

let createTeardown f td =
  Some (fun () ->
    let devicesObj = Fable.Import.JS.JSON.parse "{\"badkey\": \"badval\"}"
    f devicesObj

    td()
  )

let vagrantSetup =
  SetupTeardown(
    vagrantStart,
    vagrantDestroy
  )

testList "Info Event" [
  let withSetup f () =
    let data = "{ \"ACTION\": \"info\" }";

    commandBuilder {
      let! _ = vagrantSetup

      let! _ =
        SetupTeardown(
          vagrantRunCommand "echo 'first test is fine'",
          vagrantRunCommand "echo 'teardown for command 1'"
        )

      let! _ =
        SetupTeardown(
          vagrantRunCommand "echo 'second test is fine'",
          vagrantRunCommand "echo 'teardown for command 2'"
        )

      let! _ =
        SetupTeardown(
          vagrantRunCommand "sdfg",
          vagrantRunCommand "echo 'teardown for command 3'"
        )

      let! _ = Setup(vagrantRunCommand "udevadm trigger")

      let! socatCommandResult =
        Setup(
          vagrantPipeToShellCommand (sprintf "echo '%s'" data) ("socat - UNIX-CONNECT:/var/run/device-scanner.sock")
        )

      let devices =
        match socatCommandResult with
        | Ok(Stdout(out), _) -> out.Replace("default::\n", "")
        | Error(_) -> "{}"

      let devicesObj = Fable.Import.JS.JSON.parse devices
      printfn "devicesobj %A" devicesObj
      f devicesObj
    }
      |> Promise.map(function
        | Ok _ -> ()
        | Error (e, _, _) ->
          raise (e :?> System.Exception)
        )

  yield! testFixtureAsync withSetup [
    "should call end", fun (startResult) ->
      let expectedDevices = "{
  \"/devices/pci0000:00/0000:00:0d.0/ata4/host3/target3:0:0/3:0:0:0/block/sdb\": {
    \"ACTION\": \"add\",
    \"MAJOR\": \"8\",
    \"MINOR\": \"16\",
    \"DEVLINKS\": \"/dev/disk/by-id/ata-VBOX_HARDDISK_081118FC1221NCJ6G8GG /dev/disk/by-path/pci-0000:00:0d.0-ata-2.0\",
    \"PATHS\": [
      \"/dev/sdb\",
      \"/dev/disk/by-id/ata-VBOX_HARDDISK_081118FC1221NCJ6G8GG\",
      \"/dev/disk/by-path/pci-0000:00:0d.0-ata-2.0\"
    ],
    \"DEVNAME\": \"/dev/sdb\",
    \"DEVPATH\": \"/devices/pci0000:00/0000:00:0d.0/ata4/host3/target3:0:0/3:0:0:0/block/sdb\",
    \"DEVTYPE\": \"disk\",
    \"ID_VENDOR\": null,
    \"ID_MODEL\": \"VBOX_HARDDISK\",
    \"ID_SERIAL\": \"VBOX_HARDDISK_081118FC1221NCJ6G8GG\",
    \"ID_FS_TYPE\": null,
    \"ID_PART_ENTRY_NUMBER\": null,
    \"IML_SIZE\": \"1048576000\",
    \"IML_SCSI_80\": \"SATA     VBOX HARDDISK   081118FC1221NCJ6G8GG\",
    \"IML_SCSI_83\": \"1ATA     VBOX HARDDISK                           081118FC1221NCJ6G8GG\",
    \"IML_IS_RO\": false
  },
  \"/devices/pci0000:00/0000:00:0d.0/ata3/host2/target2:0:0/2:0:0:0/block/sda\": {
    \"ACTION\": \"add\",
    \"MAJOR\": \"8\",
    \"MINOR\": \"0\",
    \"DEVLINKS\": \"/dev/disk/by-id/ata-VBOX_HARDDISK_091118FC1221NCJ6G8GG /dev/disk/by-path/pci-0000:00:0d.0-ata-1.0\",
    \"PATHS\": [
      \"/dev/sda\",
      \"/dev/disk/by-id/ata-VBOX_HARDDISK_091118FC1221NCJ6G8GG\",
      \"/dev/disk/by-path/pci-0000:00:0d.0-ata-1.0\"
    ],
    \"DEVNAME\": \"/dev/sda\",
    \"DEVPATH\": \"/devices/pci0000:00/0000:00:0d.0/ata3/host2/target2:0:0/2:0:0:0/block/sda\",
    \"DEVTYPE\": \"disk\",
    \"ID_VENDOR\": null,
    \"ID_MODEL\": \"VBOX_HARDDISK\",
    \"ID_SERIAL\": \"VBOX_HARDDISK_091118FC1221NCJ6G8GG\",
    \"ID_FS_TYPE\": null,
    \"ID_PART_ENTRY_NUMBER\": null,
    \"IML_SIZE\": \"81920000\",
    \"IML_SCSI_80\": \"SATA     VBOX HARDDISK   091118FC1221NCJ6G8GG\",
    \"IML_SCSI_83\": \"1ATA     VBOX HARDDISK                           091118FC1221NCJ6G8GG\",
    \"IML_IS_RO\": false
  },
  \"/devices/virtual/block/dm-0\": {
    \"ACTION\": \"add\",
    \"MAJOR\": \"253\",
    \"MINOR\": \"0\",
    \"DEVLINKS\": \"/dev/centos/root /dev/disk/by-id/dm-name-centos-root /dev/disk/by-id/dm-uuid-LVM-UPOYcrJATlxeJ0Cwqvnt9FPnvHbkkwNyUbg0FbYv7A8ydd9ygP3pxlhPcvhHAcHY /dev/disk/by-uuid/b085695c-789b-44ff-a264-e6a15a3b1900 /dev/mapper/centos-root\",
    \"PATHS\": [
      \"/dev/dm-0\",
      \"/dev/centos/root\",
      \"/dev/disk/by-id/dm-name-centos-root\",
      \"/dev/disk/by-id/dm-uuid-LVM-UPOYcrJATlxeJ0Cwqvnt9FPnvHbkkwNyUbg0FbYv7A8ydd9ygP3pxlhPcvhHAcHY\",
      \"/dev/disk/by-uuid/b085695c-789b-44ff-a264-e6a15a3b1900\",
      \"/dev/mapper/centos-root\"
    ],
    \"DEVNAME\": \"/dev/dm-0\",
    \"DEVPATH\": \"/devices/virtual/block/dm-0\",
    \"DEVTYPE\": \"disk\",
    \"ID_VENDOR\": null,
    \"ID_MODEL\": null,
    \"ID_SERIAL\": null,
    \"ID_FS_TYPE\": \"xfs\",
    \"ID_PART_ENTRY_NUMBER\": null,
    \"IML_SIZE\": \"77709312\",
    \"IML_SCSI_80\": \"SATA     VBOX HARDDISK   091118FC1221NCJ6G8GG\",
    \"IML_SCSI_83\": \"1ATA     VBOX HARDDISK                           091118FC1221NCJ6G8GG\",
    \"IML_IS_RO\": false
  },
  \"/devices/virtual/block/dm-1\": {
    \"ACTION\": \"add\",
    \"MAJOR\": \"253\",
    \"MINOR\": \"1\",
    \"DEVLINKS\": \"/dev/centos/swap /dev/disk/by-id/dm-name-centos-swap /dev/disk/by-id/dm-uuid-LVM-UPOYcrJATlxeJ0Cwqvnt9FPnvHbkkwNydHbfPWJwbStUS3q2PuHEsXmZtuBPXafD /dev/disk/by-uuid/a9a3d277-a0e9-4c85-b550-ed3bf2eec439 /dev/mapper/centos-swap\",
    \"PATHS\": [
      \"/dev/dm-1\",
      \"/dev/centos/swap\",
      \"/dev/disk/by-id/dm-name-centos-swap\",
      \"/dev/disk/by-id/dm-uuid-LVM-UPOYcrJATlxeJ0Cwqvnt9FPnvHbkkwNydHbfPWJwbStUS3q2PuHEsXmZtuBPXafD\",
      \"/dev/disk/by-uuid/a9a3d277-a0e9-4c85-b550-ed3bf2eec439\",
      \"/dev/mapper/centos-swap\"
    ],
    \"DEVNAME\": \"/dev/dm-1\",
    \"DEVPATH\": \"/devices/virtual/block/dm-1\",
    \"DEVTYPE\": \"disk\",
    \"ID_VENDOR\": null,
    \"ID_MODEL\": null,
    \"ID_SERIAL\": null,
    \"ID_FS_TYPE\": \"swap\",
    \"ID_PART_ENTRY_NUMBER\": null,
    \"IML_SIZE\": \"2097152\",
    \"IML_SCSI_80\": \"SATA     VBOX HARDDISK   091118FC1221NCJ6G8GG\",
    \"IML_SCSI_83\": \"1ATA     VBOX HARDDISK                           091118FC1221NCJ6G8GG\",
    \"IML_IS_RO\": false
  },
  \"/devices/pci0000:00/0000:00:0d.0/ata3/host2/target2:0:0/2:0:0:0/block/sda/sda2\": {
    \"ACTION\": \"add\",
    \"MAJOR\": \"8\",
    \"MINOR\": \"2\",
    \"DEVLINKS\": \"/dev/disk/by-id/ata-VBOX_HARDDISK_091118FC1221NCJ6G8GG-part2 /dev/disk/by-id/lvm-pv-uuid-RkDvOo-Lp2v-X8ih-Tli5-BrYx-TmSG-zp5pyc /dev/disk/by-path/pci-0000:00:0d.0-ata-1.0-part2\",
    \"PATHS\": [
      \"/dev/sda2\",
      \"/dev/disk/by-id/ata-VBOX_HARDDISK_091118FC1221NCJ6G8GG-part2\",
      \"/dev/disk/by-id/lvm-pv-uuid-RkDvOo-Lp2v-X8ih-Tli5-BrYx-TmSG-zp5pyc\",
      \"/dev/disk/by-path/pci-0000:00:0d.0-ata-1.0-part2\"
    ],
    \"DEVNAME\": \"/dev/sda2\",
    \"DEVPATH\": \"/devices/pci0000:00/0000:00:0d.0/ata3/host2/target2:0:0/2:0:0:0/block/sda/sda2\",
    \"DEVTYPE\": \"partition\",
    \"ID_VENDOR\": null,
    \"ID_MODEL\": \"VBOX_HARDDISK\",
    \"ID_SERIAL\": \"VBOX_HARDDISK_091118FC1221NCJ6G8GG\",
    \"ID_FS_TYPE\": \"LVM2_member\",
    \"ID_PART_ENTRY_NUMBER\": 2,
    \"IML_SIZE\": \"79820800\",
    \"IML_SCSI_80\": \"SATA     VBOX HARDDISK   091118FC1221NCJ6G8GG\",
    \"IML_SCSI_83\": \"1ATA     VBOX HARDDISK                           091118FC1221NCJ6G8GG\",
    \"IML_IS_RO\": false
  },
  \"/devices/pci0000:00/0000:00:0d.0/ata3/host2/target2:0:0/2:0:0:0/block/sda/sda1\": {
    \"ACTION\": \"add\",
    \"MAJOR\": \"8\",
    \"MINOR\": \"1\",
    \"DEVLINKS\": \"/dev/disk/by-id/ata-VBOX_HARDDISK_091118FC1221NCJ6G8GG-part1 /dev/disk/by-path/pci-0000:00:0d.0-ata-1.0-part1 /dev/disk/by-uuid/76eef891-8102-4ba3-b1af-6070378aefc8\",
    \"PATHS\": [
      \"/dev/sda1\",
      \"/dev/disk/by-id/ata-VBOX_HARDDISK_091118FC1221NCJ6G8GG-part1\",
      \"/dev/disk/by-path/pci-0000:00:0d.0-ata-1.0-part1\",
      \"/dev/disk/by-uuid/76eef891-8102-4ba3-b1af-6070378aefc8\"
    ],
    \"DEVNAME\": \"/dev/sda1\",
    \"DEVPATH\": \"/devices/pci0000:00/0000:00:0d.0/ata3/host2/target2:0:0/2:0:0:0/block/sda/sda1\",
    \"DEVTYPE\": \"partition\",
    \"ID_VENDOR\": null,
    \"ID_MODEL\": \"VBOX_HARDDISK\",
    \"ID_SERIAL\": \"VBOX_HARDDISK_091118FC1221NCJ6G8GG\",
    \"ID_FS_TYPE\": \"xfs\",
    \"ID_PART_ENTRY_NUMBER\": 1,
    \"IML_SIZE\": \"2097152\",
    \"IML_SCSI_80\": \"SATA     VBOX HARDDISK   091118FC1221NCJ6G8GG\",
    \"IML_SCSI_83\": \"1ATA     VBOX HARDDISK                           091118FC1221NCJ6G8GG\",
    \"IML_IS_RO\": false
  }
}"
      let expectedDevicesObj = Fable.Import.JS.JSON.parse expectedDevices
      expect.Invoke(startResult).toEqual(expectedDevicesObj)
  ]
]
