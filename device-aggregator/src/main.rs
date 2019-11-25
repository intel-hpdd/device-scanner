// Copyright (c) 2019 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use device_aggregator::{
    aggregator_error,
    cache::{Cache, CacheFlush},
    linux_plugin_transforms::{
        build_device_lookup, devtree2linuxoutput, get_shared_pools, populate_zpool, LinuxPluginData,
    },
};
use device_types::{devices::Device, message::Message};
use futures::Future;
use std::{
    collections::{BTreeMap, HashMap},
    env::{var, VarError},
    net::ToSocketAddrs,
    sync::{Arc, Mutex},
};
use warp::Filter;

fn to_local_host(_: VarError) -> Result<String, String> {
    Ok("127.0.0.1".to_string())
}

fn main() -> Result<(), aggregator_error::Error> {
    env_logger::builder().default_format_timestamp(false).init();

    let cache = Arc::new(Mutex::new(Cache::default()));

    let cache_fut =
        warp::any().and_then(move || CacheFlush::new(cache.clone()).map_err(warp::reject::custom));

    log::info!("Server starting");

    let log = warp::log("device_aggregator");

    let host: String = var("PROXY_HOST")
        .or_else(to_local_host)
        .expect("Couldn't parse host.");
    let port: u16 = var("DEVICE_AGGREGATOR_PORT")
        .expect("DEVICE_AGGREGATOR_PORT environment variable is required.")
        .parse()
        .expect("could not parse DEVICE_AGGREGATOR_PORT to u16");

    let addr = format!("{}:{}", host, port)
        .to_socket_addrs()
        .expect("Couldn't parse address.")
        .next()
        .expect("Couldn't convert to a socket address.");

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

            let entries = cache.entries();

            let mut xs: BTreeMap<&String, _> = entries
                .iter()
                .map(|(k, v)| {
                    let mut out = LinuxPluginData::default();

                    devtree2linuxoutput(&v, None, &mut out);

                    (k, out)
                })
                .collect();

            let (path_index, cluster_pools): (HashMap<&String, _>, HashMap<&String, _>) = entries
                .iter()
                .map(|(k, v)| {
                    let mut path_to_mm = BTreeMap::new();
                    let mut pools = BTreeMap::new();

                    build_device_lookup(v, &mut path_to_mm, &mut pools);

                    ((k, path_to_mm), (k, pools))
                })
                .unzip();

            for (&h, x) in xs.iter_mut() {
                let path_to_mm = &path_index[h];
                let shared_pools = get_shared_pools(&h, path_to_mm, &cluster_pools);

                for (a, b) in shared_pools {
                    populate_zpool(a, b, x);
                }
            }

            serde_json::to_string(&xs).unwrap()
        });

    let routes = post.or(get).with(log);

    warp::serve(routes).run(addr);

    Ok(())
}
