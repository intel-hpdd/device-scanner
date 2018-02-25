# scanner-proxy-daemon

A persistent process that forwards scanner updates received on local socket to the device aggregator over HTTPS.

## Overview

This service listens on the device-scanner socket and when data is received, encapsulates and transmits over an authenticated HTTPS connection to the device-aggregator.
