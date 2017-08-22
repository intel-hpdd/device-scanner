# block-device-listener

A [net.Socket](https://nodejs.org/api/net.html) client that is started when it receives a udev event for {ADD|REMOVE} of block devices.

The client will write to the unix domain socket of `device-scanner-daemon` with the new event data. It acts as a middleman.

Udev event data is written over [process.env](https://nodejs.org/api/process.html) to this module.
