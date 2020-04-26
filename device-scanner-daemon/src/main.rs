// Copyright (c) 2019 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use device_scanner_daemon::daemon;
use futures::channel::mpsc;
use libsystemd::activation::receive_descriptors;
use std::{
    convert::TryFrom,
    io::{Error, ErrorKind},
    os::unix::{
        io::{FromRawFd, IntoRawFd},
        net::UnixListener as NetUnixListener,
    },
};
use structopt::StructOpt;
use tokio::net::UnixListener;
use tracing_subscriber::{filter::LevelFilter, fmt::Subscriber, EnvFilter};

#[derive(Debug, StructOpt)]
struct Opt {
    ///Loggin level: off, error, warn, info, debug, trace
    #[structopt(short, long, default_value = "info")]
    log_level: LevelFilter,
    /// Try to detect and listen on the socket opened by systemd
    #[structopt(long)]
    systemd: bool,
    /// Open and listen on this UNIX socket
    #[structopt(long, required_unless = "systemd")]
    socket: Option<String>,
}

async fn listen_systemd() -> Result<UnixListener, Box<dyn std::error::Error>> {
    let fds = receive_descriptors(true)?;
    if fds.is_empty() {
        Err(Box::new(Error::new(
            ErrorKind::NotFound,
            "No systemd socket opened",
        )))
    } else {
        let fd = fds[0].clone().into_raw_fd();
        let addr = unsafe { NetUnixListener::from_raw_fd(fd) };
        Ok(UnixListener::try_from(addr)?)
    }
}

async fn listen_unix(path: &str) -> Result<UnixListener, Box<dyn std::error::Error>> {
    let _ = tokio::fs::remove_file(path).await;
    Ok(UnixListener::bind(path)?)
}

async fn start() -> Result<(), Box<dyn std::error::Error>> {
    let opts = Opt::from_args();

    let subscriber = Subscriber::builder()
        .with_env_filter(EnvFilter::from_default_env())
        .with_max_level(opts.log_level)
        .finish();

    tracing::subscriber::set_global_default(subscriber).unwrap();

    let listener = if opts.systemd {
        listen_systemd().await?
    } else {
        let path = opts.socket.as_deref().unwrap();
        listen_unix(path).await?
    };

    let addr = listener.local_addr()?;
    let path = addr.as_pathname().unwrap().to_str().unwrap();

    tracing::info!("Server starting on {}", path);

    let (tx, rx) = mpsc::unbounded();

    tokio::spawn(daemon::writer(rx));

    daemon::reader(listener, tx).await?;

    Ok(())
}

#[tokio::main]
async fn main() {
    if let Err(e) = start().await {
        eprintln!("{}", e);
        std::process::exit(1);
    }
}
