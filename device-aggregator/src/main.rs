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
    dag::{build_dag, Weight},
    env::get_var,
};

use std::time::Instant;

use daggy::{
    petgraph::{dot::Dot, visit::IntoNodeReferences},
    Dag,
};

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

    // let connect_string = get_connect_string();

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
                    log::debug!("Got new data from host {}. Data: {:?}", host_name, &device);
                    let mut cache = cache.lock().unwrap();
                    cache.upsert(&host_name, device);

                    tx.clone().unbounded_send(cache.entries()).unwrap();
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

                let mut dag: Dag<Device, Weight> =
                    x.into_iter().fold(Dag::new(), |mut dag, (host, xs)| {
                        let (scsis, other): (
                            Vec<devices::Device>,
                            Vec<devices::Device>,
                        ) = xs.into_iter().partition(|x| match x {
                            Device::ScsiDevice(_) => true,
                            _ => false,
                        });

                        let id = dag.add_node(devices::Device::Host(devices::Host(host)));

                        let ids: Vec<_> = scsis.into_iter().map(|x| dag.add_node(x)).collect();

                        for i in ids {
                            dag.update_edge(id, i, Weight).unwrap();
                        }

                        for x in other {
                            dag.add_node(x);
                        }

                        dag
                    });

                let graph = dag.graph().clone();

                let scsis = graph
                    .node_references()
                    .filter(|(_, x)| match x {
                        Device::ScsiDevice(_) => true,
                        _ => false,
                    }).map(|(x, _)| x);

                for idx in scsis {
                    let cloned_dag = dag.clone();
                    let graph = cloned_dag.graph();

                    let d = &graph[idx];

                    build_dag(&mut dag, &graph, d, idx).unwrap();
                }

                let elapsed = now.elapsed();
                log::debug!(
                    "Built graph in {} ms",
                    (elapsed.as_secs() * 1_000) + u64::from(elapsed.subsec_millis())
                );

                let dag3 = dag.clone();

                let g2 = dag3.map(|_, n| n.short_display(), |_, e| e);

                let gviz = Dot::with_config(&g2, &[]);

                let mut file = File::create("/tmp/gvis").unwrap();
                file.write_all(format!("{:?}", gviz).as_ref());

                // let conn = PgConnection::establish(&connect_string.as_str())?;

                // conn.transaction::<_, diesel::result::Error, _>(|| {
                //     for dag in dags {}
                //     Ok(())
                // });

                /* 
                    Each host contains a subtree of devices. We can use serials to map child devices to any parents across the cluster.
                */

                Ok(())
            }).map_err(|e| log::error!("Unhandled Error: {:?}", e)),
    );

    rt.shutdown_on_idle().wait().unwrap();

    Ok(())
}
