extern crate device_scanner_daemon;
extern crate futures;
extern crate tokio;
extern crate tokio_mockstream;

use device_scanner_daemon::connections::{handler, Command};
use tokio::{prelude::*, runtime::Runtime};
use tokio_mockstream::MockStream;

use std::io::{self, Error, ErrorKind, Read, Write};

#[derive(Debug)]
enum MockStreams {
    FailMockStream(MockStream),
    MockStream(MockStream),
}

impl MockStreams {
    fn empty() -> Self {
        MockStreams::MockStream(MockStream::empty())
    }
    fn empty_fail() -> Self {
        MockStreams::FailMockStream(MockStream::empty())
    }
    fn written(&self) -> &[u8] {
        match self {
            MockStreams::MockStream(x) => x.written(),
            MockStreams::FailMockStream(x) => &[],
        }
    }
}

impl Read for MockStreams {
    fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
        match self {
            MockStreams::MockStream(x) | MockStreams::FailMockStream(x) => x.read(buf),
        }
    }
}

impl AsyncRead for MockStreams {}

impl Write for MockStreams {
    fn write(&mut self, buf: &[u8]) -> io::Result<usize> {
        match self {
            MockStreams::MockStream(x) => x.write(buf),
            MockStreams::FailMockStream(x) => Err(Error::new(ErrorKind::Other, "oh no!")),
        }
    }

    fn flush(&mut self) -> io::Result<()> {
        match self {
            MockStreams::MockStream(x) => x.flush(),
            MockStreams::FailMockStream(x) => Err(Error::new(ErrorKind::Other, "boom")),
        }
    }
}

impl AsyncWrite for MockStreams {
    fn shutdown(&mut self) -> Poll<(), io::Error> {
        match self {
            MockStreams::MockStream(x) | MockStreams::FailMockStream(x) => x.shutdown(),
        }
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

    let s = MockStreams::empty_fail();

    tx.unbounded_send(Command::Write("hello".to_string()))
        .unwrap();
    tx.unbounded_send(Command::Add(s)).unwrap();
    drop(tx);

    let runtime = Runtime::new().unwrap();
    let state = runtime.block_on_all(fut).unwrap();

    assert_eq!(state.conns.len(), 0);
    assert_eq!(state.msg, Some("hello".to_string()));
}

#[test]
fn test_fail_many() {
    let (tx, fut) = handler();

    let s1 = MockStreams::empty_fail();
    let s2 = MockStreams::empty();
    let s3 = MockStreams::empty_fail();

    tx.unbounded_send(Command::Add(s1)).unwrap();
    tx.unbounded_send(Command::Add(s2)).unwrap();
    tx.unbounded_send(Command::Add(s3)).unwrap();
    tx.unbounded_send(Command::Write("hello".to_string()))
        .unwrap();
    tx.unbounded_send(Command::Write("there".to_string()))
        .unwrap();

    drop(tx);

    let runtime = Runtime::new().unwrap();
    let state = runtime.block_on_all(fut).unwrap();

    assert_eq!(state.conns.len(), 1);
    assert_eq!(state.conns[0].written(), b"hellothere");
    assert_eq!(state.msg, Some("there".to_string()));
}
