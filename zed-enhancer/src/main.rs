// Copyright (c) 2019 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

//! zed-enhancer -- upgrades incoming ZED events with additional information
//!
//! ZED (ZFS Event Daemon) provides changes to state in ZFS. However it is currently
//! light in the amount of information provided when state changes.
//!
//! This crate receives events from device-scanner-zedlets and may enhance them with further data
//! before passing onwards to device-scanner.

use std::os::unix::{io::FromRawFd, net::UnixListener as NetUnixListener};
use tokio::{net::UnixListener, prelude::*, reactor::Handle};
use zed_enhancer::processor;

fn main() {
    env_logger::builder().default_format_timestamp(false).init();

    let addr = unsafe { NetUnixListener::from_raw_fd(3) };

    let listener = UnixListener::from_std(addr, &Handle::default())
        .expect("Unable to bind Unix Domain Socket fd");

    let server = listener
        .incoming()
        .map_err(|e| log::error!("accept failed, {:?}", e))
        .for_each(move |socket| {
            tokio::spawn(processor(socket).map_err(|e| log::error!("Unhandled Error: {:?}", e)))
        });

    log::info!("Server starting");

    let mut runtime = tokio::runtime::current_thread::Runtime::new().unwrap();

    runtime.block_on(server).unwrap();
}
