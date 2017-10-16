# device-scanner

[![Build Status](https://travis-ci.org/intel-hpdd/device-scanner.svg?branch=master)](https://travis-ci.org/intel-hpdd/device-scanner)
[![Greenkeeper badge](https://badges.greenkeeper.io/intel-hpdd/device-scanner.svg)](https://greenkeeper.io/)
[![codecov](https://codecov.io/gh/intel-hpdd/device-scanner/branch/master/graph/badge.svg)](https://codecov.io/gh/intel-hpdd/device-scanner)

Builds an in-memory representation of devices. Uses [udev](http://www.reactivated.net/writing_udev_rules.html) rules to handle change events.

## Quick Setup Instructions
1. build with `dotnet fable yarn-build`
1. vagrant up
1. vagrant ssh
1. `$ sudo -i`
1. `$ yum install socat`
1. `$ yum install jq`
1. `$udevadm trigger --action=change --subsystem-match=block`
1. `$ echo '{ "ACTION": "info" }' | socat - UNIX-CONNECT:/var/run/device-scanner.sock | jq`
