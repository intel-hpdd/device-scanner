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
    yield "ACTION     TARGET     SOURCE    FSTYPE OPTIONS                  OLD-TARGET OLD-OPTIONS\n"
    yield "mount      /mnt/part1 /dev/sde1 ext4   rw,relatime,data=ordered\n"
  }
    |> streamTap

testAsync "poll umount" <| fun () ->
  streams {
    yield "ACTION     TARGET          SOURCE         FSTYPE OPTIONS        OLD-TARGET      OLD-OPTIONS\n"
    yield "umount     /testPool4      testPool4      zfs    rw,xattr,noacl /testPool4      rw,xattr,noacl\n"
  }
    |> streamTap

// mount /mnt/part1 -o remount,ro
testAsync "poll remount" <| fun () ->
  streams {
    yield "ACTION     TARGET     SOURCE    FSTYPE OPTIONS                  OLD-TARGET OLD-OPTIONS\n"
    yield "remount    /mnt/part1 /dev/sde1 ext4   ro,relatime,data=ordered            rw,relatime,data=ordered\n"
  }
    |> streamTap

testAsync "poll move" <| fun () ->
  streams {
    yield "ACTION     TARGET      SOURCE    FSTYPE OPTIONS                  OLD-TARGET OLD-OPTIONS\n"
    yield "move       /mnt/part1a /dev/sde1 ext4   ro,relatime,data=ordered /mnt/part1\n"
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
    yield "ACTION     TARGET          SOURCE         FSTYPE OPTIONS        OLD-TARGET      OLD-OPTIONS\n"
    yield "mount      /testPool4      testPool4      zfs    rw,xattr,noacl\n"
    yield "mount      /testPool4/home testPool4/home zfs    rw,xattr,noacl\n"
    yield "umount     /testPool4/home testPool4/home zfs    rw,xattr,noacl /testPool4/home rw,xattr,noacl\n"
    yield "umount     /testPool4      testPool4      zfs    rw,xattr,noacl /testPool4      rw,xattr,noacl\n"
  }
    |> promiseMatch

testAsync "list then poll mount" <| fun () ->
  streams {
    yield "TARGET                   SOURCE                              FSTYPE  OPTIONS\n"
    yield "/sys                     sysfs                               sysfs   rw,nosu\n"
    yield "/proc                    proc                                proc    rw,nosu\n"
    yield "/run                     tmpfs                               tmpfs   rw,nosu\n"
    yield "/                        /dev/disk/by-uuid/6fa5a72a-ba26-4588-a103-74bb6b33a763  ext4    rw,rela\n"

    yield "ACTION TARGET SOURCE FSTYPE OPTIONS OLD-TARGET OLD-OPTIONS\n"
    yield "mount      /mnt/fs-OST0002 /dev/sdd lustre ro\n"
  }
    |> promiseMatch


testAsync "list then poll umount" <| fun () ->
  streams {
    yield "TARGET                   SOURCE                              FSTYPE  OPTIONS\n"
    yield "/mnt/fs-OST0002          /dev/sdd                            lustre  ro\n"

    yield "ACTION TARGET SOURCE FSTYPE OPTIONS OLD-TARGET OLD-OPTIONS\n"
    yield "umount      /mnt/fs-OST0002 /dev/sdd lustre ro /mnt/fs-OST0002 ro\n"
  }
    |> promiseMatch


testAsync "list then poll remount" <| fun () ->
  streams {
    yield "TARGET                   SOURCE                              FSTYPE  OPTIONS\n"
    yield "/mnt/fs-OST0002          /dev/sdd                            lustre  ro\n"

    yield "ACTION TARGET SOURCE FSTYPE OPTIONS OLD-TARGET OLD-OPTIONS\n"
    yield "remount      /mnt/fs-OST0002 /dev/sdd lustre ro rw,rela\n"
  }
    |> promiseMatch


testAsync "list then poll move" <| fun () ->
  streams {
    yield "TARGET                   SOURCE                              FSTYPE  OPTIONS\n"
    yield "/mnt/fs-OST0002          /dev/sdd                            lustre  ro\n"

    yield "ACTION TARGET SOURCE FSTYPE OPTIONS OLD-TARGET OLD-OPTIONS\n"
    yield "umount      /mnt/fs-OST0002 /dev/sdd lustre ro /mnt/fs-OST0003\n"
  }
    |> promiseMatch

