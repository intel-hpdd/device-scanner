// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

//! Wraps incoming connections
//!
//! Each `Connection` exposes a `mpsc` `tx` handle which
//! allows the message queue to fanout a single message to
//! many connections.
//!
//! `Connection` also implements `Future` which is important as
//! it's pinned to each incoming socket connection.
//!
//! This allows the server to make progress writing to each connection
//! without blocking all writes on the longest one.

use futures::{
    future::Future,
    sync::mpsc::{self, UnboundedReceiver, UnboundedSender},
};
use crate::error;
use tokio::prelude::*;
use bytes::{BufMut, Bytes, BytesMut};

pub type Tx = UnboundedSender<Bytes>;

/// Takes the write-half of the `UnixStream`
/// and wraps it
///
/// A mpsc is kept internally, the tx side can be cloned
/// and used to fanout a single message to many connections.
///
pub struct Connection<T> {
    pub tx: Tx,
    rx: UnboundedReceiver<Bytes>,
    wr: BytesMut,
    pub conn: Option<T>,
}

impl<T> Connection<T>
where
    T: AsyncWrite + Send,
{
    pub fn new(conn: T) -> Self {
        let (tx, rx) = mpsc::unbounded();

        Connection {
            tx,
            rx,
            conn: Some(conn),
            wr: BytesMut::new(),
        }
    }
    /// Buffer a line.
    ///
    /// This writes the line to an internal buffer.
    /// `Connection` is also a future, this
    /// buffer will drain when the Future polls
    fn buffer(&mut self, line: &[u8]) {
        self.wr.reserve(line.len());

        self.wr.put(line);
    }
}

impl<T> Future for Connection<T>
where
    T: AsyncWrite + Send,
{
    type Item = T;
    type Error = error::Error;

    fn poll(&mut self) -> Poll<T, error::Error> {
        // Tokio (and futures) use cooperative scheduling without any
        // preemption. If a task never yields execution back to the executor,
        // then other tasks may be starved.
        //
        // To deal with this, robust applications should not have any unbounded
        // loops. In this example, we will read at most `LINES_PER_TICK` lines
        // from the client on each tick.
        //
        // If the limit is hit, the current task is notified, informing the
        // executor to schedule the task again asap.
        const LINES_PER_TICK: usize = 10;

        for i in 0..LINES_PER_TICK {
            // Polling an `UnboundedReceiver` cannot fail, so `unwrap` here is
            // safe.
            match self.rx.poll().unwrap() {
                Async::Ready(Some(v)) => {
                    self.buffer(&v);

                    // If this is the last iteration, the loop will break even
                    // though there could still be lines to read. Because we did
                    // not reach `Async::NotReady`, we have to notify ourselves
                    // in order to tell the executor to schedule the task again.
                    if i + 1 == LINES_PER_TICK {
                        task::current().notify();
                    }
                }
                _ => break,
            }
        }

        let mut c = self
            .conn
            .take()
            .ok_or_else(|| error::none_error("Tried to take a connection from None"))?;

        // Flush the write buffer to the connection
        // As long as there is buffered data to write, try to write it.
        while !self.wr.is_empty() {
            match c.poll_write(&self.wr) {
                Ok(Async::Ready(n)) => {
                    // As long as the wr is not empty, a successful write should
                    // never write 0 bytes.
                    assert!(n > 0);

                    // This discards the first `n` bytes of the buffer.
                    self.wr.split_to(n);
                }
                Ok(Async::NotReady) => break,
                Err(_) => {
                    // If we get *any* error on this socket, we resolve the future
                    // which closes the connection
                    return Ok(Async::Ready(c));
                }
            }
        }

        self.conn = Some(c);
        Ok(Async::NotReady)
    }
}
