// Copyright (c) 2019 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

//! A Writer that sends messages to clients in a separate thread.
//!
//! By utilizing a separate thread for writing, we ensure that any writers are not bounded by the slowest reader.


use futures::{channel::mpsc::UnboundedReceiver, future::join_all, StreamExt};
use tokio::io::{AsyncWrite, AsyncWriteExt};

pub enum WriterCmd {
    Add(Box<dyn AsyncWrite + Unpin + Send>),
    Msg(bytes::Bytes),
}

pub fn spawn_writer(mut rx: UnboundedReceiver<WriterCmd>) {
    tokio::spawn(async move {
        let mut writers = vec![];

        while let Some(cmd) = rx.next().await {
            match cmd {
                WriterCmd::Add(w) => writers.push(w),
                WriterCmd::Msg(x) => {
                    let xs = join_all(writers.iter_mut().map(|writer| writer.write_all(&x))).await;

                    writers = writers
                        .into_iter()
                        .enumerate()
                        .filter(|(idx, _)| {
                            if let Err(e) = xs[*idx].as_ref() {
                                tracing::debug!("Error writing to client {}. Removing client", e);

                                false
                            } else {
                                true
                            }
                        })
                        .map(|(_, w)| w)
                        .collect();

                    tracing::debug!("{} clients remain.", writers.len());
                }
            }
        }
    });
}
