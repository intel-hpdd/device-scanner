#![allow(unknown_lints)]
#![warn(clippy)]

extern crate device_types;
extern crate futures;
extern crate hyper;
extern crate serde;
extern crate serde_json;
extern crate tokio;

use hyper::{
    rt::{self, Future},
    Server,
};

use std::{
    env,
    net::{IpAddr, Ipv4Addr, SocketAddr},
};

mod lib;
use lib::{aggregator_error, aggregator_service, cache::Cache};

fn main() -> aggregator_error::Result<()> {
    let cache = Cache::default();
    let aggregator = aggregator_service::Aggregator::new(cache);

    println!("Starting device-aggregator");

    let port = env::var("DEVICE_AGGREGATOR_PORT")
        .expect("DEVICE_AGGREGATOR_PORT environment variable is required.");

    let socket = SocketAddr::new(
        IpAddr::V4(Ipv4Addr::new(127, 0, 0, 1)),
        port.parse().expect("Could not parse port to u16"),
    );

    let server = Server::bind(&socket)
        .serve(aggregator)
        .map_err(|e| eprintln!("server error: {}", e));

    rt::run(server);

    Ok(())
}
