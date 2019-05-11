// Copyright (c) 2019 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use device_scanner_proxy::{build_uri, send_message};
use device_types::message::Message;
use failure::{Error, ResultExt};
use futures::{future::Future, stream::Stream};
use futures_failure::{print_cause_chain, FutureExt, StreamExt};
use lazy_static::lazy_static;
use std::{
    env,
    io::BufReader,
    path::Path,
    process::Command,
    time::{Duration, Instant},
};
use tokio::{
    io::{lines, write_all},
    net::UnixStream,
    timer::Interval,
};

/// Checks if the given path exists in the FS
///
/// # Arguments
///
/// * `name` - The path to check
fn path_exists(name: &str) -> bool {
    Path::new(name).exists()
}

fn required(name: &str) -> String {
    env::var(name).unwrap_or_else(|_| panic!("Expected {} to be supplied in ENV", name))
}

fn get_private_pem_path() -> String {
    required("PRIVATE_PEM_PATH")
}

fn get_cert_path() -> String {
    required("CRT_PATH")
}

fn get_pfx_path() -> String {
    required("PFX_PATH")
}

fn get_authority_cert_path() -> String {
    required("AUTHORITY_CRT_PATH")
}

/// Gets the pfx file.
/// If pfx is not found it will be created.
lazy_static! {
    pub static ref PFX: Vec<u8> = {
        let private_pem_path = get_private_pem_path();

        if !path_exists(&private_pem_path) {
            panic!("{} does not exist", private_pem_path)
        };

        let cert_path = get_cert_path();

        if !path_exists(&cert_path) {
            panic!("{} does not exist", cert_path)
        }

        let authority_cert_path = get_authority_cert_path();

        let pfx_path = get_pfx_path();

        if !path_exists(&pfx_path) {
            Command::new("openssl")
                .args(&[
                    "pkcs12",
                    "-export",
                    "-out",
                    &pfx_path,
                    "-inkey",
                    &private_pem_path,
                    "-in",
                    &cert_path,
                    "-certfile",
                    &authority_cert_path,
                    "-passout",
                    "pass:",
                ])
                .status()
                .expect("Error creating pfx");
        }

        std::fs::read(&pfx_path).expect("Could not read pfx")
    };
}

fn main() -> Result<(), Error> {
    let uri = required("IML_MANAGER_URL");
    let uri = build_uri(&uri)?;
    // Clone so we can use in each task
    let uri2 = uri.clone();

    println!("Starting device-scanner-proxy server");

    // Send a heartbeat every 10s to let the device-aggregator know this
    // node is still alive.
    let timer = Interval::new(Instant::now(), Duration::from_secs(10))
        .context("While creating interval")
        .and_then(|_| {
            serde_json::to_string(&Message::Heartbeat)
                .context("Expected Heartbeat to serialize")
                .map_err(Error::from)
        })
        .map_err(|e| {
            print_cause_chain(&e);
        })
        .for_each(move |json| {
            tokio::spawn(send_message(&uri, json, &PFX).map_err(|e| {
                print_cause_chain(&e);
            }))
        });

    let stream = UnixStream::connect("/var/run/device-scanner.sock")
        .context("Connecting to device-scanner.sock")
        .and_then(|conn| write_all(conn, "\"Stream\"\n").context("Writing to the Stream"))
        .map(|(c, _)| lines(BufReader::new(c)).context("Error reading line"))
        .flatten_stream()
        .map(Message::Data)
        .and_then(|msg| {
            serde_json::to_string::<Message>(&msg)
                .context("Expected Message to serialize")
                .map_err(Error::from)
        })
        .map_err(|e| print_cause_chain(&e))
        .for_each(move |json| {
            tokio::spawn(send_message(&uri2, json, &PFX).map_err(|e| {
                print_cause_chain(&e);
            }))
        });

    let mut runtime = tokio::runtime::Runtime::new().expect("Tokio runtime start failed");

    runtime.spawn(stream);
    runtime.spawn(timer);

    runtime
        .shutdown_on_idle()
        .wait()
        .expect("Failed waiting on runtime");

    Ok(())
}
