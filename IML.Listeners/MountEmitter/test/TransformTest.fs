module IML.MountEmitter.TransformTest

open Fable.Import.Jest
open Matchers
open Fable.Import.Node.PowerPack.Stream
open Transform
open IML.Types.CommandTypes

testAsync "header then mount" <| fun () ->
  streams {
    yield "ACTION TARGET SOURCE FSTYPE OPTIONS\n"

    yield "mount      /mnt/fs-OST0002 /dev/sdd lustre ro\n"
  }
    |> transform
    |> tap (fun xs ->
      xs == (
        {
          target = (Mount.MountPoint "/mnt/fs-OST0002");
          source = (Mount.BdevPath "/dev/sdd");
          fstype = (Mount.FsType "lustre");
          opts = (Mount.MountOpts "ro")
        } |> Mount |> Command.MountCommand
      )
    )
    |> Util.streamToPromise

testAsync "header then umount" <| fun () ->
  streams {
    yield "ACTION TARGET SOURCE FSTYPE OPTIONS\n"

    yield "umount      /mnt/fs-OST0002 /dev/sdd lustre ro\n"
  }
    |> transform
    |> tap (fun xs ->
      xs == (
        {
          target = (Mount.MountPoint "/mnt/fs-OST0002");
          source = (Mount.BdevPath "/dev/sdd");
          fstype = (Mount.FsType "lustre");
          opts = (Mount.MountOpts "ro")
        } |> Umount |> Command.MountCommand
      )
    )
    |> Util.streamToPromise

testAsync "list mounts after header" <| fun () ->
  streams {
    yield "TARGET SOURCE FSTYPE OPTIONS\n"

    yield "/mnt/fs-OST0002 /dev/sdd lustre ro\n"
  }
    |> transform
    |> tap (fun xs ->
      xs == (
        {
          target = (Mount.MountPoint "/mnt/fs-OST0002");
          source = (Mount.BdevPath "/dev/sdd");
          fstype = (Mount.FsType "lustre");
          opts = (Mount.MountOpts "ro")
        } |> Mount |> Command.MountCommand
      )
    )
    |> Util.streamToPromise
