// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use std::env;

/// Get the environment variable or panic
pub fn get_var(name: &str) -> String {
    env::var(name).unwrap_or_else(|_| format!("{} environment variable is required.", name))
}
