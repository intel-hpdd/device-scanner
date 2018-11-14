#![allow(unknown_lints)]
#![warn(clippy)]

extern crate device_types;
extern crate futures;
extern crate serde;
extern crate serde_json;
extern crate tokio;
extern crate warp;

use device_types::{devices::Device, message::Message};
use warp::Filter;

use futures::prelude::*;

use std::{
    env,
    sync::{Arc, Mutex},
};

mod lib;
use lib::{
    aggregator_error,
    cache::{Cache, CacheFlush},
};

fn main() -> aggregator_error::Result<()> {
    let cache = Arc::new(Mutex::new(Cache::default()));

    let cache_fut =
        warp::any().and_then(move || CacheFlush::new(cache.clone()).map_err(warp::reject::custom));

    println!("Starting device-aggregator");

    let port: u16 = env::var("DEVICE_AGGREGATOR_PORT")
        .expect("DEVICE_AGGREGATOR_PORT environment variable is required.")
        .parse()
        .expect("could not parse DEVICE_AGGREGATOR_PORT to u16");

    let post = warp::post2()
        .and(warp::body::json())
        .and(warp::header::<String>("x-ssl-client-name"))
        .and(cache_fut.clone())
        .map(
            |m: Message, host_name: String, cache: Arc<Mutex<Cache>>| match m {
                Message::Heartbeat => {
                    println!("in heartbeat for {:?}", host_name);
                    let cache = cache.clone();
                    cache.lock().unwrap().reset(&host_name);
                }
                Message::Data(d) => {
                    let device: Device = serde_json::from_str(&d).unwrap();
                    println!("in data for {:?}, {:?}", host_name, d);
                    cache.lock().unwrap().upsert(&host_name, device);
                }
            },
        ).map(|_| warp::reply::with_status("", warp::http::StatusCode::CREATED));

    let get = warp::get2()
        .and(cache_fut.clone())
        .map(|cache: Arc<Mutex<Cache>>| {
            let cache = cache.clone();
            let cache = cache.lock().unwrap();
            cache.entries()
        }).map(|x| warp::reply::json(&x));

    let routes = post.or(get);

    warp::serve(routes).run(([127, 0, 0, 1], port));

    Ok(())
}
