// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

extern crate daggy;
extern crate device_types;

extern crate diesel;
extern crate env_logger;
extern crate futures;
extern crate im;
extern crate log;
extern crate serde;
extern crate serde_json;
extern crate tokio;
extern crate warp;

use device_types::{
    devices::{self, Device},
    message::Message,
};

use std::io::prelude::*;
use warp::Filter;

use futures::{sync::mpsc, Future, Stream};

use std::{
    fs::File,
    sync::{Arc, Mutex},
};

extern crate device_aggregator;

use device_aggregator::{
    aggregator_error,
    cache::{Cache, CacheFlush},
    dag::{self, add_shared_edges, populate_parents},
    db,
    env::get_var,
};

use std::time::Instant;

use daggy::petgraph::dot::Dot;

fn main() -> aggregator_error::Result<()> {
    env_logger::init();

    let cache = Arc::new(Mutex::new(Cache::default()));

    let cache_fut =
        warp::any().and_then(move || CacheFlush::new(cache.clone()).map_err(warp::reject::custom));

    log::info!("Server starting");

    let log = warp::log("device_aggregator");

    let port: u16 = get_var("DEVICE_AGGREGATOR_PORT")
        .parse()
        .expect("could not parse DEVICE_AGGREGATOR_PORT to u16");

    let connect = db::connector();

    let (tx, rx) = mpsc::unbounded();

    let post = warp::post2()
        .and(warp::body::json())
        .and(warp::header::<String>("x-ssl-client-name"))
        .and(cache_fut.clone())
        .map(
            move |m: Message, host_name: String, cache: Arc<Mutex<Cache>>| match m {
                Message::Heartbeat => {
                    log::debug!("Got a new heartbeat from host {}", host_name);
                    let cache = cache.clone();
                    cache.lock().unwrap().reset(&host_name);
                }
                Message::Data(d) => {
                    let device: im::HashSet<Device> = serde_json::from_str(&d).unwrap();
                    log::debug!("Got data from host {}", host_name);
                    let mut cache = cache.lock().unwrap();

                    let last_entries = cache.entries();
                    let is_same = last_entries
                        .get(&host_name)
                        .filter(|&last_device| &device == last_device);

                    if is_same.is_none() {
                        log::debug!("Got new data from host {}. Data: {:?}", host_name, &device);

                        cache.upsert(&host_name, device);

                        tx.clone().unbounded_send(cache.entries()).unwrap();
                    }
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

    let routes = post.or(get).with(log);

    let service = warp::serve(routes);

    let (_, fut) = service.bind_ephemeral(([127, 0, 0, 1], port));

    let mut rt = tokio::runtime::Runtime::new().unwrap();

    rt.spawn(fut);

    rt.spawn(
        rx.map_err(|()| -> warp::Error { unreachable!("unbounded rx never errors") })
            .for_each(|x| {
                let now = Instant::now();

                let mut dag: dag::Dag = x
                    .into_iter()
                    .map(|(host, xs)| {
                        let mut dag = dag::Dag::new();

                        let id = dag.add_node(devices::Device::Host(devices::Host(host)));

                        for x in xs {
                            dag.add_node(x);
                        }

                        let ro_dag = dag.clone();

                        populate_parents(&mut dag, &ro_dag, id).unwrap();

                        (id, dag)
                    }).try_fold(
                        dag::Dag::new(),
                        |mut l, (id, r)| -> aggregator_error::Result<dag::Dag> {
                            dag::add(&mut l, &r, id)?;

                            Ok(l)
                        },
                    ).unwrap();

                add_shared_edges(&mut dag).unwrap();

                let elapsed = now.elapsed();
                log::debug!(
                    "Built graph in {} ms",
                    (elapsed.as_secs() * 1_000) + u64::from(elapsed.subsec_millis())
                );

                let gviz = Dot::new(&dag);

                let mut file = File::create("/tmp/gvis").unwrap();
                file.write_all(format!("{}", gviz).as_ref()).unwrap();

                let xs: Vec<_> = dag::into_device_set(&dag)
                    .into_iter()
                    .filter_map(db::create_records_from_device_and_hosts)
                    .collect();

                log::debug!("The records I want to insert: {:?}", xs);

                let elapsed = now.elapsed();
                log::debug!(
                    "Built db records in {} ms",
                    (elapsed.as_secs() * 1_000) + u64::from(elapsed.subsec_millis())
                );

                let mut file = std::fs::OpenOptions::new()
                    .append(true)
                    .open("/tmp/finished")
                    .unwrap();

                writeln!(
                    file,
                    "finished in {}",
                    (elapsed.as_secs() * 1_000) + u64::from(elapsed.subsec_millis())
                ).unwrap();

                // let conn = connect()?;

                // conn.transaction::<_, diesel::result::Error, _>(|| Ok(()));

                /* 
                    Each host contains a subtree of devices. We can use serials to map child devices to any parents across the cluster.
                */

                Ok(())
            }).map_err(|e| log::error!("Unhandled Error: {:?}", e)),
    );

    rt.shutdown_on_idle().wait().unwrap();

    Ok(())
}
