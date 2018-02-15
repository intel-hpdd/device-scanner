# device-scanner-daemon

A persistent process that forwards scanner updates received on local socket to the device aggregator over HTTPS.

## Overview

This service lessons on the device-scanner socket and when data is received it is encapsulated and sent over an authenticated HTTPS connection to the IML manager.
