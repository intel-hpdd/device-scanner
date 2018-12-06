// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

table! {
    device (device_type, serial) {
        device_type -> VarChar,
        serial -> VarChar,
        size -> BigInt,
        fs_type -> Nullable<VarChar>,
    }
}

table! {
    device_host (device_type, device_serial, host_fqdn) {
        device_type -> VarChar,
        device_serial -> VarChar,
        host_fqdn -> VarChar,
        paths -> Array<VarChar>,
        mount_path -> Nullable<VarChar>,
        is_active -> Bool,
    }
}
