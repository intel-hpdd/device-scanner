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

use std::{os::unix::{io::FromRawFd, net::UnixListener as NetUnixListener}, convert::TryFrom};
use tokio::net::UnixListener;
use futures::TryStreamExt;
use zed_enhancer::processor;
use tracing_subscriber::{fmt::Subscriber, EnvFilter};

#[tokio::main(single_thread)]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let subscriber = Subscriber::builder()
        .with_env_filter(EnvFilter::from_default_env())
        .finish();

    tracing::subscriber::set_global_default(subscriber).unwrap();

    tracing::info!("Server started");
    
    let addr = unsafe { NetUnixListener::from_raw_fd(3) };

    let listener = UnixListener::try_from(addr)?;

    let mut stream = listener.incoming();

    while let Some(socket) = stream.try_next().await? {
        processor(socket).await?;
    }

    Ok(())
}
