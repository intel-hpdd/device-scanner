//! A fake stream for testing network applications backed by buffers.
#![warn(missing_docs)]

extern crate futures;
extern crate tokio;

use futures::Poll;
use std::io;
use std::io::{Cursor, Read, Write};
use tokio::prelude::*;

/// A fake stream for testing network applications backed by buffers.
#[derive(Clone, Debug)]
pub struct MockStream {
    pub written: Cursor<Vec<u8>>,
    received: Cursor<Vec<u8>>,
}

impl MockStream {
    /// Creates a new mock stream with nothing to read.
    pub fn empty() -> MockStream {
        MockStream::new(&[])
    }

    /// Creates a new mock stream with the specified bytes to read.
    pub fn new(initial: &[u8]) -> MockStream {
        MockStream {
            written: Cursor::new(vec![]),
            received: Cursor::new(initial.to_owned()),
        }
    }

    /// Gets a slice of bytes representing the data that has been written.
    pub fn written(&self) -> &[u8] {
        self.written.get_ref()
    }
}

impl Read for MockStream {
    fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
        self.received.read(buf)
    }
}

impl AsyncRead for MockStream {}

impl Write for MockStream {
    fn write(&mut self, buf: &[u8]) -> io::Result<usize> {
        let _ = self.written.write(buf)?;
        Err(io::Error::new(io::ErrorKind::BrokenPipe, "Pipe has broke"))
    }

    fn flush(&mut self) -> io::Result<()> {
        self.written.flush()?;
        Err(io::Error::new(io::ErrorKind::BrokenPipe, "Pipe has broke"))
    }
}

impl AsyncWrite for MockStream {
    fn shutdown(&mut self) -> Poll<(), io::Error> {
        self.written.shutdown()
    }
}
