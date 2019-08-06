// Copyright (c) 2019 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use device_scanner_daemon::{connections, error, state};
use device_types::Command;
use futures::{sync::mpsc::UnboundedSender, Future, Stream};
use std::{
    io::BufReader,
    net::Shutdown,
    os::unix::{io::FromRawFd, net::UnixListener as NetUnixListener},
};
use tokio::{io::lines, net::UnixListener, net::UnixStream, reactor::Handle};

/// Reads the first line off a `UnixStream` and converts it to a `Command`.
///
/// Returns an `Option` of `(device_types::Command, UnixStream)` or `None`
/// if there was no line to read.
fn read_first_line(
    socket: UnixStream,
) -> impl Future<Item = Option<(device_types::Command, UnixStream)>, Error = error::Error> {
    lines(BufReader::new(socket))
        .into_future()
        .map_err(|(e, _)| e.into())
        .and_then(|(x, socket_wrapped)| match x {
            Some(x) => {
                let cmd = serde_json::from_str::<Command>(x.trim_end()).map_err(|e| {
                    log::error!("Could not parse command. Tried to parse: {}, got: {}", x, e);
                    e
                })?;

                let socket = socket_wrapped.into_inner().into_inner();

                log::debug!("Incoming Command: {:?}", cmd);

                Ok(Some((cmd, socket)))
            }
            None => Ok(None),
        })
}

fn build_connection(
    (cmd, socket): (device_types::Command, UnixStream),
    message_tx: UnboundedSender<(Command, connections::Tx)>,
) -> Result<connections::Connection, error::Error> {
    match cmd {
        Command::GetMounts => {
            let connection = connections::Connection::new(socket, true);

            message_tx
                .clone()
                .unbounded_send((cmd, connection.tx.clone()))?;

            Ok(connection)
        }
        Command::Stream => {
            let connection = connections::Connection::new(socket, false);

            message_tx
                .clone()
                .unbounded_send((cmd, connection.tx.clone()))?;

            Ok(connection)
        }
        _ => {
            socket.shutdown(Shutdown::Both)?;

            let connection = connections::Connection::new(socket, false);

            message_tx
                .clone()
                .unbounded_send((cmd, connection.tx.clone()))?;

            Ok(connection)
        }
    }
}

fn main() {
    env_logger::builder().default_format_timestamp(false).init();

    let (message_tx, state_fut) = state::handler();

    let addr = unsafe { NetUnixListener::from_raw_fd(3) };

    let listener = UnixListener::from_std(addr, &Handle::default())
        .expect("Unable to bind Unix Domain Socket fd");

    let server = listener
        .incoming()
        .from_err()
        .and_then(read_first_line)
        .filter_map(std::convert::identity)
        .and_then(move |x| build_connection(x, message_tx.clone()))
        .map_err(|e| log::error!("Unhandled Error: {:?}", e))
        .for_each(move |connection| {
            tokio::spawn(
                connection
                    .map(drop)
                    .map_err(|e| log::error!("Unhandled Error: {:?}", e)),
            )
        });

    log::info!("Server starting");

    let mut runtime = tokio::runtime::Builder::new()
        .panic_handler(|err| std::panic::resume_unwind(err))
        .build()
        .expect("Tokio runtime failed to start");

    runtime.spawn(server);
    runtime.spawn(
        state_fut
            .map(|_| ())
            .map_err(|e| log::error!("Unhandled Error: {:?}", e)),
    );

    runtime
        .shutdown_on_idle()
        .wait()
        .expect("Failed to shutdown runtime");
}
