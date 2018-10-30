#![allow(unknown_lints)]
#![warn(clippy)]

extern crate failure;
extern crate futures;
extern crate futures_failure;
extern crate hyper;
extern crate native_tls;
extern crate serde;
extern crate serde_json;
extern crate tokio;
extern crate tokio_tls;

extern crate device_types;

mod lib;

use std::{
    env, fs,
    io::BufReader,
    time::{Duration, Instant},
};

use failure::{Error, ResultExt};
use futures::future::Future;
use futures_failure::{print_cause_chain, FutureExt, StreamExt};

use tokio::{
    io::{lines, write_all},
    net::UnixStream,
    prelude::*,
    timer::Interval,
};

use device_types::message::Message;

use lib::{build_uri, send_message};

fn required(name: &str) -> String {
    env::var(name).unwrap_or_else(|_| panic!("Expected {} to be supplied in ENV", name))
}

fn main() -> Result<(), Error> {
    let uri = required("IML_MANAGER_URL");
    let uri = build_uri(&uri)?;
    // Clone so we can use in each task
    let uri2 = uri.clone();

    let pfx = required("IML_CERT_PFX");
    let pfx = fs::read(pfx)?;
    // Clone so we can use in each task
    let pfx2 = pfx.clone();

    println!("Starting device-scanner-proxy server");

    // Send a heartbeat every 10s to let the device-aggregator know this
    // node is still alive.
    let timer = Interval::new(Instant::now(), Duration::from_secs(10))
        .context("While creating interval")
        .and_then(|_| {
            serde_json::to_string(&Message::Heartbeat)
                .context("Expected Heartbeat to serialize")
                .map_err(Error::from)
        }).for_each(move |json| {
            send_message(&uri, json, &pfx).or_else(|e| {
                print_cause_chain(&e);

                future::ok(())
            })
        }).map_err(|e| {
            print_cause_chain(&e);
        });

    let stream = UnixStream::connect("/var/run/device-scanner.sock")
        .context("Connecting to device-scanner.sock")
        .and_then(move |conn| {
            let (read, write) = conn.split();

            write_all(write, "\"Stream\"\n")
                .context("Writing to the Stream")
                .and_then(move |_| {
                    lines(BufReader::new(read))
                        .context("Error reading line")
                        .map(Message::Data)
                        .and_then(|msg| {
                            serde_json::to_string::<Message>(&msg)
                                .context("Expected Message to serialize")
                                .map_err(Error::from)
                        }).for_each(move |json| {
                            send_message(&uri2, json, &pfx2).or_else(|e| {
                                print_cause_chain(&e);

                                future::ok(())
                            })
                        })
                })
        }).map_err(|e| print_cause_chain(&e));

    let mut runtime = tokio::runtime::Runtime::new().expect("Tokio runtime start failed");

    runtime.spawn(stream);
    runtime.spawn(timer);

    runtime
        .shutdown_on_idle()
        .wait()
        .expect("Failed waiting on runtime");

    Ok(())
}
