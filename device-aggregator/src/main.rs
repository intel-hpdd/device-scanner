// Copyright (c) 2019 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use device_aggregator::{
    aggregator_error,
    cache::{Cache, CacheFlush},
    linux_plugin_transforms::{devtree2linuxoutput, LinuxPluginData},
};
use device_types::{devices::Device, message::Message};
use futures::Future;
use std::{
    collections::BTreeMap,
    env,
    sync::{Arc, Mutex},
};
use warp::Filter;

fn main() -> Result<(), aggregator_error::Error> {
    env_logger::init();

    let cache = Arc::new(Mutex::new(Cache::default()));

    let cache_fut =
        warp::any().and_then(move || CacheFlush::new(cache.clone()).map_err(warp::reject::custom));

    log::info!("Server starting");

    let log = warp::log("device_aggregator");

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
                    let cache = cache.clone();
                    cache.lock().unwrap().reset(&host_name);
                }
                Message::Data(d) => {
                    let device: Device = serde_json::from_str(&d).unwrap();
                    cache.lock().unwrap().upsert(&host_name, device);
                }
            },
        )
        .map(|_| warp::reply::with_status("", warp::http::StatusCode::CREATED));

    let get = warp::get2()
        .and(cache_fut.clone())
        .map(|cache: Arc<Mutex<Cache>>| {
            let cache = cache.lock().unwrap();

            // Build out top-level structure.
            // Check for shared items and add where needed.

            let entries = cache.entries();

            let xs: BTreeMap<&String, _> = entries
                .iter()
                .map(|(k, v)| {
                    let mut out = LinuxPluginData::default();

                    devtree2linuxoutput(&v, None, &mut out);

                    (k, out)
                })
                .collect();

            serde_json::to_string(&xs).unwrap()
        });

    let routes = post.or(get).with(log);

    warp::serve(routes).run(([127, 0, 0, 1], port));

    Ok(())
}
