#![allow(unknown_lints)]
#![warn(clippy)]

extern crate im;

extern crate device_scanner_daemon;
extern crate device_types;
extern crate failure;
extern crate futures;
extern crate futures_failure;
extern crate serde;
extern crate serde_json;
extern crate tokio;

use device_scanner_daemon::{connections, state};

use failure::{Error, ResultExt};
use futures_failure::{print_cause_chain, FutureExt};

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

fn processor(
    socket: UnixStream,
    message_tx: UnboundedSender<(Command, UnboundedSender<connections::Command<UnixStream>>)>,
    connections_tx: UnboundedSender<connections::Command<UnixStream>>,
) -> impl Future<Item = (), Error = Error> {
    lines(BufReader::new(socket))
        .into_future()
        .map_err(|(e, _)| e)
        .context("While reading lines")
        .and_then(|(x, socket_wrapped)| {
            // If `x` is `None`, then the client disconnected without sending a line of data
            let x: String = match x {
                Some(x) => x,
                None => return Either::A(future::ok(None)),
            };

            // Parse the command. If it's invalid, we simply coerce to None, and print the error.
            // This will short-circuit the rest of the future chain.
            let cmd: Command = match serde_json::from_str::<Command>(&x) {
                Ok(c) => c,
                Err(e) => {
                    eprintln!("Could not parse command. Tried to parse: {}, got: {}", x, e);

                    return Either::A(future::ok(None));
                }
            };

            let socket = socket_wrapped.into_inner().into_inner();

            let output = (cmd, socket);

            Either::B(future::ok(Some(output)))
        }).and_then(move |x| match x {
            Some((cmd, socket)) => {
                if let Command::Stream = cmd {
                    connections_tx
                        .unbounded_send(connections::Command::Add(socket))
                        .context("Connection send failed")
                        .map_err(Error::from)
                } else {
                    socket
                        .shutdown(Shutdown::Both)
                        .context("Socket shutdown failed")?;

                    message_tx
                        .unbounded_send((cmd, connections_tx))
                        .context("Message send failed")
                        .map_err(Error::from)
                }
            }
            None => Ok(()),
        })
}

fn main() {
    let (message_tx, state_fut) = state::handler();

    let (connections_tx, connections_fut) = connections::handler();

    let addr = unsafe { NetUnixListener::from_raw_fd(3) };

    let listener = UnixListener::from_std(addr, &Handle::default())
        .expect("Unable to bind Unix Domain Socket fd");

    let server = listener
        .incoming()
        .map_err(|e| eprintln!("accept failed, {:?}", e))
        .for_each(move |socket| {
            tokio::spawn(
                processor(socket, message_tx.clone(), connections_tx.clone())
                    .map_err(|e| print_cause_chain(&e)),
            )
        });

    println!("Server starting");

    let mut runtime = tokio::runtime::Runtime::new().expect("Tokio runtime start failed");

    runtime.spawn(
        future::select_all(vec![
            Box::new(server) as Box<Future<Item = (), Error = ()> + Send>,
            Box::new(state_fut.map(|_| ()).map_err(|e| print_cause_chain(&e))),
            Box::new(connections_fut.map(|_| ())),
        ]).map(|_| ())
        .map_err(|_| ()),
    );

    runtime
        .shutdown_on_idle()
        .wait()
        .expect("Failed waiting on runtime");
}
