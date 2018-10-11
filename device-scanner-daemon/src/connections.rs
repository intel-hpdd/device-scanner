//! Holds a list of active `Connections` that want updates whenever data changes.
//!
//! Each connection holds a queue of messages to be written.

use futures::future::{Either, Future};
use futures::sync::mpsc::{self, UnboundedSender};

use tokio::{io::write_all, net::UnixStream, prelude::*};

/// Messages for the connection handler.
/// `Add` will push a new `UnixStream` for writing.
/// `Remove` will remove the `UnixStream` at the specified
/// index. `Write` takes a message to send to all held connections.
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

type ActiveStreams = Vec<UnixStream>;

struct State {
    conns: ActiveStreams,
    msg: String,
}

impl State {
    fn new() -> Self {
        State {
            conns: vec![],
            msg: "".to_string(),
        }
    }
}

/// Sets up connection handling through a mpsc channel
/// Returns a `tx` handle to send and a `future` to attach to the tokio runtime
pub fn handler() -> (
    UnboundedSender<(Command)>,
    impl Future<Item = (), Error = ()>,
) {
    let (tx, rx) = mpsc::unbounded();

    let fut = rx
        .fold(
            State::new(),
            |State { mut conns, msg }: State, cmd| match cmd {
                Command::Add(s) => {
                    let fut = write_all(s, msg.clone())
                        .then(move |x| match x {
                            Ok((s, _)) => {
                                conns.push(s);

                                future::ok(conns)
                            }
                            Err(_) => future::ok(conns),
                        }).map(|conns| State { conns, msg });

                    Either::A(fut)
                }
                Command::Write(msg) => {
                    let xs: Vec<_> = conns
                        .drain(0..)
                        .map(|v| write_to_stream(v, msg.clone()))
                        .collect();

                    let fut = future::join_all(xs)
                        .map(|xs| xs.into_iter().filter_map(|s| s).collect())
                        .map(|conns| State { conns, msg });

                    Either::B(fut)
                }
            },
        ).map(|_| ());

    (tx, fut)
}
