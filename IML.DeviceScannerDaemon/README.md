# device-scanner-daemon(8) -- emits block device + ZFS info

## SYNOPSIS

This module utilizes [`Udev`](http://www.reactivated.net/writing_udev_rules.html), `ZED`, and `libzfs` to construct an in-memory representation of block devices, zpools, zfs objects and properties. It utilizes unix domain sockets to allow connections / near real-time updates of the device tree as it changes over time. This module is used by [Intel Manager for Lustre](https://github.com/intel-hpdd/intel-manager-for-lustre) to receive changes to block-devices / zfs data as it occurs. It can also be used stand-alone in modular applications.

Internally, [@iml/node-libzfs](https://github.com/intel-hpdd/rust-libzfs/tree/master/node-libzfs) is used to complete extra data from ZED.

There are two main modes to this daemon:

1. Processing new incoming events.

2. Emit current devices. In this mode we will emit our current stored info as changes occur.

Data can be fetched over unix domain socket by sending the JSON message `"Info"` to the running socket at `/var/run/device-scanner.sock`.

Example using `socat`:

```shell
echo '"Info"' | socat - UNIX-CONNECT:/var/run/device-scanner.sock
```

It's the responsibility of the caller to end the connection when finished. While the caller leaves the connection open, the `device-scanner` will emit changes (the current device tree) as they are processed. This can be useful for push-based processing.
