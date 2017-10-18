# device-scanner-daemon

A persistent process that consumes udev events over a unix domain socket.

## Overview

There are two main modes to this daemon:

1. Processing new incoming events. In this mode we will munge and store incoming events.

2. Send current devices object listing. In this mode we will send our current stored info.

We use unix domain sockets to communicate with the outside world.

Consumers can send a JSON Request over the socket to retrieve the current listing:

`{ "ACTION": "info" }`


## Architecture

```
         ┌───────────┐
         │   Udev    │
         └───────────┘
               │
               │
               │
       ┌───────┴────────┐
       │   Run Script   │
       └───────┬────────┘
               │
               │
               ▼
  ┌─────────────────────────┐
  │  Block-Device Listener  │
  └─────────────────────────┘
               │
               │
               │
               │
   ┌───────────┴──────────┐
   │  Unix Domain Socket  │
   └───────────┬──────────┘
               │
               │
               ▼
┌────────────────────────────┐
│   Device Scanner Daemon    │
└────────────────────────────┘
               ▲
               │
               │
               │
    ┌──────────┴──────────┐
    │ Unix Domain Socket  │
    └──────────┬──────────┘
               │
               │
               │
               ▼
     ┌──────────────────┐
     │ Consumer Process │
     └──────────────────┘
```
