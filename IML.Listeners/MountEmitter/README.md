# IML.MountEmitter

This module listens to polling changes from `findmnt`.

It then pipes those changes to [device-scanner](../IML.DeviceScannerDaemon) where they are consumed
under a `mounts` key.
