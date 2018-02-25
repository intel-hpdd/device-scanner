# device-scanner

[![Build Status](https://travis-ci.org/intel-hpdd/device-scanner.svg?branch=master)](https://travis-ci.org/intel-hpdd/device-scanner)
[![Greenkeeper badge](https://badges.greenkeeper.io/intel-hpdd/device-scanner.svg)](https://greenkeeper.io/)

This repo provides a [persistent daemon](IML.DeviceScannerDaemon) That holds block devices + ZFS devices in memory.

It also provides [listeners](IML.Listeners) that emit changes to the daemon.

Finally, it also provides a proxy [proxy](IML.ScannerProxyDaemon) that transforms the unix domain socket events to HTTP POSTs.

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
