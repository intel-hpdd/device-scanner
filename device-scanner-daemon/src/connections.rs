//! Handles Incoming Connections
//! `device-scanner` uses a persistent streaming strategy
//! where Unix domain sockets can connect and be fed device-graph changes as they occur.
//! the sockets are responsible for closing their end when they are done listening for updates.

use futures::{
    future::Future,
    sync::mpsc::{self, UnboundedSender},
};

use tokio::{io::write_all, net::UnixStream, prelude::*};

type ActiveStreams = Vec<UnixStream>;

struct State {
    conns: ActiveStreams,
    msg: Option<String>,
}

impl State {
    fn new() -> Self {
        State {
            conns: vec![],
            msg: None,
        }
    }
}

/// Messages for the connection handler.
/// `Add` will push a new `UnixStream` for writing.
/// `Remove` will remove the `UnixStream` at the specified
/// index.
pub enum Command {
    Add(UnixStream),
    Write(String),
}

fn write_to_stream(
    stream: UnixStream,
    msg: String,
) -> impl Future<Item = Option<UnixStream>, Error = ()> {
    write_all(stream, msg).then(move |x| match x {
        Ok((stream, _)) => future::ok(Some(stream)),
        Err(_) => future::ok(None),
    })
}

/// Sets up connection handling through a mpsc channel
/// Returns a `tx` handle to send and a `future` to attach to the tokio runtime
/// Internally, the `rx` end of this `mpsc` is folded over
/// and holds it's own connection state.
/// A new `Vec` of connections is constructed from each `Command::Write`,
/// with any errored connections filtered out of the `Vec`.
pub fn handler() -> (
    UnboundedSender<(Command)>,
    impl Future<Item = (), Error = ()>,
) {
    let (tx, rx) = mpsc::unbounded();

    let fut = rx
        .fold(
            State::new(),
            |State { mut conns, msg }: State, cmd| match cmd {
                Command::Add(s) => match msg {
                    None => {
                        conns.push(s);

                        Box::new(future::ok(State { conns, msg: None }))
                            as Box<Future<Item = State, Error = ()> + Send>
                    }
                    Some(msg) => {
                        let fut = write_all(s, msg.clone())
                            .then(|x| match x {
                                Ok((s, _)) => {
                                    conns.push(s);

                                    future::ok(conns)
                                }
                                Err(_) => future::ok(conns),
                            }).map(|conns| State {
                                conns,
                                msg: Some(msg),
                            });

                        Box::new(fut)
                    }
                },
                Command::Write(msg) => {
                    let xs: Vec<_> = conns
                        .drain(0..)
                        .map(|v| write_to_stream(v, msg.clone()))
                        .collect();

                    let fut = future::join_all(xs)
                        .map(|xs| xs.into_iter().filter_map(|s| s).collect())
                        .map(|conns| State {
                            conns,
                            msg: Some(msg),
                        });

                    Box::new(fut)
                }
            },
        ).map(|_| ());

    (tx, fut)
}
