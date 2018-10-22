# device-scanner

[![Build Status](https://travis-ci.org/whamcloud/device-scanner.svg?branch=master)](https://travis-ci.org/whamcloud/device-scanner)

This repo provides:

- a [persistent daemon](device-scanner-daemon) That holds block devices, ZFS devices, and device mounts in memory.
- a [binary](uevent-listener) that emits UEvents for block-devices as they occur.
- a [binary](mount-emitter) that emits device mount changes as they occur.
- a [proxy](device-scanner-proxy) that transforms the unix domain socket events to HTTP POSTs.

## Architecture

```
    ┌───────────────┐ ┌───────────────┐
    │  Udev Script  │ │    ZEDlet     │
    └───────────────┘ └───────────────┘
            │                 │
            └────────┬────────┘
                     ▼
          ┌─────────────────────┐
          │ Unix Domain Socket  │
          └─────────────────────┘
                     │
                     ▼
       ┌───────────────────────────┐
       │   Device Scanner Daemon   │
       └───────────────────────────┘
                     │
                     ▼
          ┌─────────────────────┐
          │ Unix Domain Socket  │
          └─────────────────────┘
                     │
                     ▼
           ┌──────────────────┐
           │ Consumer Process │
           └──────────────────┘
```

## Development Dependencies

- [rust](https://www.rust-lang.org/)
- [ZFS](https://zfsonlinux.org/) Optional
- [Vagrant](https://www.vagrantup.com) Optional
- [Virtualbox](https://www.virtualbox.org/) Optional

## Development setup

- (Optional) Install ZFS via OS package manager
- Install Rust deps: `cargo build`

### Building the app

#### Local

- `cargo build`

#### Vagrant

- Running `vagrant up` will setup a complete environment. It will build `device-scanner`, `scanner-proxy`, and `device-aggregator`, package them as RPMs and install them on the correct nodes.

  To interact with the device-scanner in real time the following command can be used to keep the stream open such that updates can be seen as the data changes:

  ```shell
  cat - | ncat -U /var/run/device-scanner.sock | jq
  ```

  If interaction is not required, device info can be retrieved from the device-scanner by running the following command:

  ```shell
  echo '"Stream"' | socat - UNIX-CONNECT:/var/run/device-scanner.sock | jq
  ```

### Testing the app

- `cargo test`
