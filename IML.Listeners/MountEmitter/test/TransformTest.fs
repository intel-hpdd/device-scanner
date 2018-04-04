module IML.MountEmitter.TransformTest

open Fable.Import.Jest
open Fable.Import.Node.PowerPack.Stream
open Transform
open Fable.PowerPack

let streamTap =
  transform
    >> tap (fun xs -> expect.Invoke(xs).toMatchSnapshot())
    >> Util.streamToPromise

let promiseMatch =
  transform
    >> Util.streamToPromise
    >> Promise.map (fun xs -> expect.Invoke(xs).toMatchSnapshot())

testAsync "poll mount" <| fun () ->
  streams {
    yield "ACTION TARGET SOURCE FSTYPE OPTIONS\n"
    yield "mount      /mnt/fs-OST0002 /dev/sdd lustre ro\n"
  }
    |> streamTap

testAsync "poll umount" <| fun () ->
  streams {
    yield "ACTION TARGET SOURCE FSTYPE OPTIONS\n"
    yield "umount      /mnt/fs-OST0002 /dev/sdd lustre ro\n"
  }
    |> streamTap

testAsync "list mount" <| fun () ->
  streams {
    yield "TARGET SOURCE FSTYPE OPTIONS\n"
    yield "/mnt/fs-OST0002 /dev/sdd lustre ro\n"
  }
    |> streamTap

testAsync "poll mount then umount" <| fun () ->
  streams {
    yield "ACTION TARGET SOURCE FSTYPE OPTIONS\n"
    yield "mount       /mnt/fs-OST0002 /dev/sdd lustre ro\n"
    yield "umount      /mnt/fs-OST0002 /dev/sdd lustre ro\n"
  }
    |> promiseMatch

testAsync "list then poll" <| fun () ->
  streams {
    yield "TARGET                   SOURCE                              FSTYPE  OPTIONS\n"
    yield "/sys                     sysfs                               sysfs   rw,nosu\n"
    yield "/proc                    proc                                proc    rw,nosu\n"
    yield "/run                     tmpfs                               tmpfs   rw,nosu\n"
    yield "/                        /dev/disk/by-uuid/6fa5a72a-ba26-4588-a103-74bb6b33a763  ext4    rw,rela\n"
    yield "/mnt/fs-OST0002          /dev/sdd                            lustre  ro\n"

    yield "ACTION TARGET SOURCE FSTYPE OPTIONS\n"
    yield "umount      /mnt/fs-OST0002 /dev/sdd lustre ro\n"
  }
    |> promiseMatch

