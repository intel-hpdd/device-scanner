#![allow(unknown_lints)]
#![warn(clippy)]

#[cfg(test)]
#[macro_use]
extern crate pretty_assertions;

#[macro_use]
extern crate im;

extern crate futures;
extern crate serde;
extern crate serde_json;

extern crate tokio;

extern crate device_types;

mod connections;
mod state;

use std::{
    io::BufReader,
    net::Shutdown,
    os::unix::{io::FromRawFd, net::UnixListener as NetUnixListener},
};

use tokio::{io::lines, net::UnixListener, prelude::*, reactor::Handle};

use futures::{
    future::{self, Either},
    sync::mpsc::UnboundedSender,
};

use device_types::Command;

fn processor<'a>(
    socket: tokio::net::UnixStream,
    message_tx: UnboundedSender<(Command, UnboundedSender<connections::Command>)>,
    connections_tx: UnboundedSender<connections::Command>,
) -> impl Future<Item = (), Error = ()> + 'a {
    lines(BufReader::new(socket))
        .into_future()
        .map_err(|(e, _)| eprintln!("error reading lines: {}", e))
        .and_then(|(x, lines)| {
            // If `x` is `None`, then the client disconnected without sending a line of data
            let x: String = match x {
                Some(x) => x,
                None => return Either::A(future::ok(None)),
            };

            // Parse the command. If it's invalid, we simply cooerce to None, and print the error.
            // This will short-circuit the rest of the future chain.
            let cmd: Command = match serde_json::from_str::<Command>(&x) {
                Ok(c) => c,
                Err(e) => {
                    eprintln!("Could not parse command. Tried to parse: {}, got: {}", x, e);

                    return Either::A(future::ok(None));
                }
            };

            let socket = lines.into_inner().into_inner();

            let output = (cmd, socket);

            Either::B(future::ok(Some(output)))
        }).map(move |x| {
            if let Some((cmd, socket)) = x {
                if let Command::Stream = cmd {
                    connections_tx
                        .unbounded_send(connections::Command::Add(socket))
                        .expect("expected connection send to work")
                } else {
                    socket.shutdown(Shutdown::Both).unwrap();

                    message_tx
                        .unbounded_send((cmd, connections_tx))
                        .expect("expected message send to work")
                };
            }
        })
}

fn main() {
    let (message_tx, state_fut) = state::handler();

    let (connections_tx, connections_fut) = connections::handler();

    let mut runtime = tokio::runtime::Runtime::new().expect("could not create runtime");

    let addr = unsafe { NetUnixListener::from_raw_fd(3) };

    let listener = UnixListener::from_std(addr, &Handle::default())
        .expect("Unable to bind Unix Domain Socket fd");

    let server = listener
        .incoming()
        .map_err(|e| eprintln!("accept failed = {}", e))
        .for_each(move |socket| {
            tokio::spawn(processor(
                socket,
                message_tx.clone(),
                connections_tx.clone(),
            ))
        }).map_err(|e| panic!("{:?}", e));

    println!("server starting");
    runtime.spawn(server);
    runtime.spawn(state_fut);
    runtime.spawn(connections_fut);

    runtime
        .shutdown_on_idle()
        .wait()
        .expect("failed waiting on runtime");
}
