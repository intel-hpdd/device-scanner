# IML.Listeners

This module contains specialized listeners for `Udev` and `Zed`.

The listeners will write to the unix domain socket of `device-scanner-daemon` with data.

Event data is written over [process.env](https://nodejs.org/api/process.html) to these modules.
