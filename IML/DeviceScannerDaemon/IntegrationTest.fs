module IML.DeviceScannerDaemon.IntegrationTest

open Fable.Core
open Fable.PowerPack
open Fable.Import.JS
open Fable.Import.Node
open Fable.Import.Jest
open Fable.Import.Jest.Matchers
open NodeHelpers
open IML.Test.VagrantTest

testList "Info Event" [
  let withSetup f () =
    let data = "{ \"ACTION\": \"info\" }";

    promise {
      let! destroyResult = vagrantDestroy()
      printfn "Finished destroying vagrant node with result %s" destroyResult
      let! startResult = vagrantStart()
      printfn "Finished loading vagrant box"
      let! socatResult = vagrantRunCommand "yum install -y socat"
      printfn "socatResult %s" socatResult
      let! triggerResult = vagrantRunCommand "udevadm trigger"
      let! socatCommandResult = vagrantPipeToShellCommand (sprintf "echo '%s'" data) ("socat - UNIX-CONNECT:/var/run/device-scanner.sock")
      printfn "socatCommandResult = %s" socatCommandResult
      let devices = socatCommandResult.Replace("default::\n", "")
      let devicesObj = JSON.parse devices
      f (devicesObj)
    }

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
      let expectedDevicesObj = JSON.parse expectedDevices
      expect.Invoke(startResult).toEqual(expectedDevicesObj)
  ]
]
