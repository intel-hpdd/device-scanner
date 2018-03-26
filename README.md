# device-scanner

[![Build Status](https://travis-ci.org/intel-hpdd/device-scanner.svg?branch=master)](https://travis-ci.org/intel-hpdd/device-scanner)
[![Greenkeeper badge](https://badges.greenkeeper.io/intel-hpdd/device-scanner.svg)](https://greenkeeper.io/)

This repo provides a [persistent daemon](IML.DeviceScannerDaemon) That holds block devices + ZFS devices in memory.

It also provides [listeners](IML.Listeners) that emit changes to the daemon.

Finally, it also provides a [proxy](IML.ScannerProxyDaemon) that transforms the unix domain socket events to HTTP POSTs.

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

## Development Requirements

* [dotnet SDK](https://www.microsoft.com/net/download/core) 2.0 or higher
* [node.js](https://nodejs.org) 6.11 or higher
* [Vagrant](https://www.vagrantup.com) Optional
* [Virtualbox](https://www.virtualbox.org/) Optional

> npm comes bundled with node.js, but we recommend to use at least npm 5. If you
> have npm installed, you can upgrade it by running `npm install -g npm`.

Although is not a Fable requirement, on macOS and Linux you'll need
[Mono](http://www.mono-project.com/) for other F# tooling like Paket or editor
support.

## Development setup

* (Optional) Install ZFS via OS package manager
* Install F# dependencies: `npm run restore`
* Install JS dependencies: `npm i`

### Building the app

#### Local

* `dotnet fable npm-build`

#### Vagrant

* Running `vagrant up` will setup a complete environment. It will build `device-scanner`, package it as an RPM and install it on the node.

  To interact with the device-scanner in real time the following command can be used to keep the stream open such that updates can be seen as the data changes:

  ```shell
  cat - | ncat -U /var/run/device-scanner.sock | jq
  ```

  If interaction is not required, device info can be retrieved from the device-scanner by running the following command:

  ```shell
  echo '"Info"' | socat - UNIX-CONNECT:/var/run/device-scanner.sock | jq
  ```

### Testing the app

* Run the tests `dotnet fable npm-test`
* Run the tests and output code coverage `dotnet fable npm-coverage`
* Run the tests in watch mode:
  * In one terminal `dotnet fable start`
  * In a second terminal `npm run test-watch`
    * This will allow you to run all, or just a subset of tests, and will
      re-test the changed files on save.
