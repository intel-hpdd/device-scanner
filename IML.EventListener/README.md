# event-listener

A [net.Socket](https://nodejs.org/api/net.html) client that is invoked by event emitting programs such as UDEV or ZED.

The client will write to the unix domain socket of `device-scanner-daemon` with the new event data. It acts as a middleman.

Event data is written over [process.env](https://nodejs.org/api/process.html) to this module.
