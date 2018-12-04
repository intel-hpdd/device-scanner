// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

table! {
    device {
        id -> Integer,
        size -> BigInt,
        device_type -> VarChar,
        serial -> VarChar,
        fs_type -> Nullable<VarChar>,
        mount_path -> Nullable<VarChar>,
    }
}

table! {
    device_host {
        id -> Integer,
        device_id -> Integer,
        paths -> Array<VarChar>,
        host_fqdn -> VarChar,
    }
}
