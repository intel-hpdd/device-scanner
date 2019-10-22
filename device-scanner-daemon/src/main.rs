// Copyright (c) 2019 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use device_scanner_daemon::{
    reducers::{mount::update_mount, udev::update_udev, zed::update_zed_events},
    state,
};
use device_types::{state::State, Command};
use futures::{future::join_all, StreamExt, TryStreamExt};
use std::{
    convert::TryFrom,
    os::unix::{io::FromRawFd, net::UnixListener as NetUnixListener},
};
use tokio::{
    codec::{FramedRead, LinesCodec},
    io::AsyncWriteExt,
    net::UnixListener,
};
use tracing_subscriber::{fmt::Subscriber, EnvFilter};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let subscriber = Subscriber::builder()
        .with_env_filter(EnvFilter::from_default_env())
        .finish();

    tracing::subscriber::set_global_default(subscriber).unwrap();

    tracing::info!("Server starting");

    let addr = unsafe { NetUnixListener::from_raw_fd(3) };

    let mut listener = UnixListener::try_from(addr)?
        .incoming()
        .inspect_ok(|_| tracing::debug!("Client connected"));

    let mut state = State::new();

    let mut writers = vec![];

    while let Some(sock) = listener.try_next().await? {
        let (reader, mut writer) = tokio::io::split(sock);

        let (x, _) = FramedRead::new(reader, LinesCodec::new())
            .into_future()
            .await;

        if let Some(x) = x {
            let cmd = serde_json::from_str::<Command>(x?.trim_end())?;

            tracing::debug!("Incoming Command: {:?}", cmd);

            match cmd {
                Command::Stream => {
                    let output = state::produce_device_graph(&state)?;

                    writer.write_all(&output).await?;

                    writers.push(writer);

                    continue;
                }
                Command::GetMounts => {
                    let v = serde_json::to_string(&state.local_mounts)?;
                    let b = bytes::BytesMut::from(v + "\n");
                    let b = b.freeze();

                    writer.write_all(&b).await?;

                    continue;
                }
                Command::UdevCommand(x) => {
                    state.uevents = update_udev(&state.uevents, x);
                }
                Command::MountCommand(x) => {
                    state.local_mounts = update_mount(state.local_mounts, x);
                }
                Command::PoolCommand(x) => {
                    state.zed_events = update_zed_events(state.zed_events, x)?
                }
            };

            let output = state::produce_device_graph(&state)?;

            let xs = join_all(writers.iter_mut().map(|writer| writer.write_all(&output))).await;

            for (i, x) in xs.iter().enumerate() {
                if x.is_err() {
                    writers.remove(i);
                }
            }
        }
    }

    Ok(())
}
