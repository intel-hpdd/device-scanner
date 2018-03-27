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
      xs == Command.MountCommand (Mount (Mount.MountPoint "/mnt/fs-OST0002", Mount.BdevPath "/dev/sdd", Mount.FsType "lustre", Mount.Options "ro"))
    )
    |> Util.streamToPromise
