extern crate device_scanner_daemon;
extern crate futures;
extern crate tokio;
extern crate tokio_mockstream;

use device_scanner_daemon::connections::{handler, Command};
use tokio::{prelude::*, runtime::Runtime};
use tokio_mockstream::MockStream;

use std::io::{self, Error, ErrorKind, Read, Write};

struct FailMockStream(MockStream);

impl FailMockStream {
    fn empty() -> Self {
        FailMockStream(MockStream::empty())
    }
}

impl Read for FailMockStream {
    fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
        self.0.read(buf)
    }
}

impl AsyncRead for FailMockStream {}

impl Write for FailMockStream {
    fn write(&mut self, _: &[u8]) -> io::Result<usize> {
        Err(Error::new(ErrorKind::Other, "oh no!"))
    }

    fn flush(&mut self) -> io::Result<()> {
        Err(Error::new(ErrorKind::Other, "boom"))
    }
}

impl AsyncWrite for FailMockStream {
    fn shutdown(&mut self) -> Poll<(), io::Error> {
        self.0.shutdown()
    }
}

#[test]
fn test_add_connection() {
    let (tx, fut) = handler();

    let s = MockStream::empty();

    tx.unbounded_send(Command::Write("hello".to_string()))
        .unwrap();
    tx.unbounded_send(Command::Add(s)).unwrap();
    drop(tx);

    let runtime = Runtime::new().unwrap();
    let state = runtime.block_on_all(fut).unwrap();

    assert_eq!(state.conns.len(), 1);
    assert_eq!(state.conns[0].written(), b"hello");
    assert_eq!(state.msg, Some("hello".to_string()));
}

#[test]
fn test_fail() {
    let (tx, fut) = handler();

    let s = FailMockStream::empty();

    tx.unbounded_send(Command::Write("hello".to_string()))
        .unwrap();
    tx.unbounded_send(Command::Add(s)).unwrap();
    drop(tx);

    let runtime = Runtime::new().unwrap();
    let state = runtime.block_on_all(fut).unwrap();

    assert_eq!(state.conns.len(), 0);
    assert_eq!(state.msg, Some("hello".to_string()));
}
