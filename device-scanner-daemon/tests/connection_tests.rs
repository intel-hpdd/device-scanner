// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

mod mock_stream;

use bytes::Bytes;
use device_scanner_daemon::{connections::Connection, error};
use tokio::runtime::Runtime;

#[test]
fn test_write_one() -> error::Result<()> {
    let s = mock_stream::MockStream::empty();

    let c = Connection::new(s);
    c.tx.unbounded_send(Bytes::from("hello\n"))?;

    let mut runtime = Runtime::new()?;
    let s = runtime.block_on(c)?;

    assert_eq!(s.written(), b"hello\n");

    Ok(())
}

#[test]
fn test_write_many() -> error::Result<()> {
    let s = mock_stream::MockStream::empty();

    let c = Connection::new(s);
    c.tx.unbounded_send(Bytes::from("hello\n"))?;
    c.tx.unbounded_send(Bytes::from("there\n"))?;

    let mut runtime = Runtime::new()?;
    let s = runtime.block_on(c)?;

    assert_eq!(s.written(), b"hello\nthere\n");

    Ok(())
}
