module IML.MountEmitter.TransformTest

open Fable.Import.Jest
open Fable.Import.Node.PowerPack.Stream
open Transform
open IML.Types.CommandTypes
open Fable.PowerPack
open Matchers

let promiseMatch =
  transform
    >> Util.streamToPromise
    >> Promise.map (List.toArray >> (Array.map Command.encoder) >> toMatchSnapshot)

testAsync "poll mount" <| fun () ->
  streams {
    yield "ACTION=\"mount\" TARGET=\"/mnt/part1\" SOURCE=\"/dev/sde1\" FSTYPE=\"ext4\" OPTIONS=\"rw,relatime,data=ordered\" OLD-TARGET=\"\" OLD-OPTIONS=\"\"\n"
  } |> promiseMatch

testAsync "poll umount" <| fun () ->
 streams {
   yield "ACTION=\"umount\" TARGET=\"/testPool4\" SOURCE=\"testPool4\" FSTYPE=\"zfs\" OPTIONS=\"rw,xattr,noacl\" OLD-TARGET=\"\" OLD-OPTIONS=\"\"\n"
 } |> promiseMatch

// mount /mnt/part1 -o remount,ro
testAsync "poll remount" <| fun () ->
  streams {
    yield "ACTION=\"remount\" TARGET=\"/mnt/part1\" SOURCE=\"/dev/sde1\" FSTYPE=\"ext4\" OPTIONS=\"ro,relatime,data=ordered\" OLD-TARGET=\"\" OLD-OPTIONS=\"rw,data=ordered\"\n"
  } |> promiseMatch

testAsync "poll move" <| fun () ->
  streams {
    yield "ACTION=\"move\" TARGET=\"/mnt/part1a\" SOURCE=\"/dev/sde1\" FSTYPE=\"ext4\" OPTIONS=\"rw,relatime,data=ordered\" OLD-TARGET=\"/mnt/part1\" OLD-OPTIONS=\"\"\n"
  } |> promiseMatch

testAsync "list mount" <| fun () ->
 streams {
   yield "TARGET=\"/mnt/part1\" SOURCE=\"/dev/sde1\" FSTYPE=\"ext4\" OPTIONS=\"rw,relatime,data=ordered\"\n"
 } |> promiseMatch

testAsync "poll mount then umount" <| fun () ->
  streams {
    yield "ACTION=\"mount\" TARGET=\"/testPool4\" SOURCE=\"testPool4\" FSTYPE=\"zfs\" OPTIONS=\"rw,xattr,noacl\" OLD-TARGET=\"\" OLD-OPTIONS=\"\"\n"
    yield "ACTION=\"mount\" TARGET=\"/testPool4/home\" SOURCE=\"testPool4/home\" FSTYPE=\"zfs\" OPTIONS=\"rw,xattr,noacl\" OLD-TARGET=\"\" OLD-OPTIONS=\"\"\n"
    yield "ACTION=\"umount\" TARGET=\"/testPool4/home\" SOURCE=\"testPool4/home\" FSTYPE=\"zfs\" OPTIONS=\"rw,xattr,noacl\" OLD-TARGET=\"/testPool4/home\" OLD-OPTIONS=\"rw,xattr,noacl\"\n"
    yield "ACTION=\"umount\" TARGET=\"/testPool4\" SOURCE=\"testPool4\" FSTYPE=\"zfs\" OPTIONS=\"rw,xattr,noacl\" OLD-TARGET=\"/testPool4\" OLD-OPTIONS=\"rw,xattr,noacl\"\n"
  } |> promiseMatch

testAsync "list then poll mount" <| fun () ->
 streams {
   yield "TARGET=\"/sys\" SOURCE=\"sysfs\" FSTYPE=\"sysfs\" OPTIONS=\"rw,nosu\"\n"
   yield "ACTION=\"mount\" TARGET=\"/mnt/fs-OST0002\" SOURCE=\"/dev/sdd\" FSTYPE=\"lustre\" OPTIONS=\"ro\" OLD-TARGET=\"\" OLD-OPTIONS=\"\"\n"
 } |> promiseMatch

testAsync "list then poll umount" <| fun () ->
  streams {
    yield "TARGET=\"/mnt/fs-OST0002\" SOURCE=\"/dev/sdd\" FSTYPE=\"lustre\" OPTIONS=\"ro\"\n"
    yield "ACTION=\"umount\" TARGET=\"/mnt/fs-OST0002\" SOURCE=\"/dev/sdd\" FSTYPE=\"lustre\" OPTIONS=\"ro\" OLD-TARGET=\"/mnt/fs-OST0002\" OLD-OPTIONS=\"ro\"\n"
  } |> promiseMatch


testAsync "list then poll remount" <| fun () ->
  streams {
    yield "TARGET=\"/mnt/fs-OST0002\" SOURCE=\"/dev/sdd\" FSTYPE=\"lustre\" OPTIONS=\"ro\"\n"
    yield "ACTION=\"remount\" TARGET=\"/mnt/fs-OST0002\" SOURCE=\"/dev/sdd\" FSTYPE=\"lustre\" OPTIONS=\"rw,rela\" OLD-TARGET=\"\" OLD-OPTIONS=\"ro\"\n"
  } |> promiseMatch


testAsync "list then poll move" <| fun () ->
  streams {
    yield "TARGET=\"/mnt/fs-OST0002\" SOURCE=\"/dev/sdd\" FSTYPE=\"lustre\" OPTIONS=\"ro\"\n"
    yield "ACTION=\"move\" TARGET=\"/mnt/fs-OST0003\" SOURCE=\"/dev/sdd\" FSTYPE=\"lustre\" OPTIONS=\"ro\" OLD-TARGET=\"/mnt/fs-OST0002\" OLD-OPTIONS=\"\"\n"
  } |> promiseMatch
