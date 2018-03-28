module IML.MountEmitterTest

open Fable.Import.Jest
open Matchers
open Fable.Import.Node.PowerPack.Stream
open IML.MountEmitter
open IML.Types.CommandTypes

testAsync "header then data" <| fun () ->
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
