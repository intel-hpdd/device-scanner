#![allow(unknown_lints)]
#![warn(clippy)]

extern crate bytes;
extern crate im;

extern crate device_types;
extern crate futures;
extern crate serde;
extern crate serde_json;
extern crate tokio;

use std::{
    io::BufReader,
    net::Shutdown,
    os::unix::{io::FromRawFd, net::UnixListener as NetUnixListener},
};

use tokio::{io::lines, net::UnixListener, net::UnixStream, prelude::*, reactor::Handle};

use futures::{
    future::{self, Either},
    sync::mpsc::UnboundedSender,
};

use device_types::Command;

extern crate device_scanner_daemon;
use device_scanner_daemon::{connections, error, state};

/// Takes an incoming socket and message tx handle
///
/// Consumes the first line of the stream
/// and parses it into a `Command`.
///
/// Wraps the socket into a `Connection` and pushes it's handle into the message tx
/// so future messages can fanout to all connections.
fn processor(
    socket: UnixStream,
    message_tx: UnboundedSender<(Command, connections::Tx)>,
) -> impl Future<Item = (), Error = error::Error> {
    lines(BufReader::new(socket))
        .into_future()
        .map_err(|(e, _)| e.into())
        .and_then(|(x, socket_wrapped)| {
            let x = x.and_then(|x| {
                serde_json::from_str::<Command>(x.trim_end())
                    .map_err(|e| {
                        eprintln!("Could not parse command. Tried to parse: {}, got: {}", x, e);
                        e
                    }).ok()
            });

            let cmd = match x {
                Some(x) => x,
                None => return Either::A(future::ok(None)),
            };

            let socket = socket_wrapped.into_inner().into_inner();

            let output = (cmd, socket);

            Either::B(future::ok(Some(output)))
        }).and_then(move |x| match x {
            Some((Command::Stream, socket)) => {
                let connection = connections::Connection::new(socket);

                message_tx
                    .clone()
                    .unbounded_send((Command::Stream, connection.tx.clone()))?;

                Ok(Some(connection))
            }
            Some((cmd, socket)) => {
                socket.shutdown(Shutdown::Both)?;

                let connection = connections::Connection::new(socket);

                message_tx
                    .clone()
                    .unbounded_send((cmd, connection.tx.clone()))?;

                Ok(Some(connection))
            }
            None => Ok(None),
        }).and_then(|x| match x {
            Some(connection) => Box::new(connection.map(|_| ()))
                as Box<Future<Item = (), Error = error::Error> + Send>,
            None => Box::new(futures::future::ok(())),
        })
}

fn main() {
    let (message_tx, state_fut) = state::handler();

    let addr = unsafe { NetUnixListener::from_raw_fd(3) };

    let listener = UnixListener::from_std(addr, &Handle::default())
        .expect("Unable to bind Unix Domain Socket fd");

    let server = listener
        .incoming()
        .map_err(|e| eprintln!("accept failed, {:?}", e))
        .for_each(move |socket| {
            tokio::spawn(processor(socket, message_tx.clone()).map_err(|e| ()))
        });

    println!("Server starting");

    let mut runtime = tokio::runtime::Runtime::new().expect("Tokio runtime start failed");

    runtime.spawn(server);
    runtime.spawn(state_fut.map(|_| ()).map_err(|e| ()));

    runtime
        .shutdown_on_idle()
        .wait()
        .expect("Failed waiting on runtime");
}
