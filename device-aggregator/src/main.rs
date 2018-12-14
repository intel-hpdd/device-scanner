// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use std::sync::{Arc, Mutex};

use device_types::{devices::Device, message::Message};

use warp::Filter;

use futures::{sync::mpsc, Future, Stream};

use device_aggregator::{
    aggregator_error::{Error, Result},
    cache::{Cache, CacheFlush},
    dag,
    env::get_var,
};

use std::time::Instant;

use daggy::petgraph::dot::Dot;

fn main() -> Result<()> {
    env_logger::init();

    let cache = Arc::new(Mutex::new(Cache::default()));

    let cache_fut =
        warp::any().and_then(move || CacheFlush::new(cache.clone()).map_err(warp::reject::custom));

    log::info!("Server starting");

    let log = warp::log("device_aggregator");

    let port: u16 = get_var("DEVICE_AGGREGATOR_PORT")
        .parse()
        .expect("could not parse DEVICE_AGGREGATOR_PORT to u16");

    // let connect = db::connector();

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
                    let is_same = last_entries.get(&host_name).filter(|&last_device| {
                        device.is_subset(last_device) && last_device.is_subset(&device)
                    });

                    if is_same.is_none() {
                        log::debug!("Got new data from host {}. Data: {:?}", host_name, &device);

                        cache.upsert(&host_name, device);

                        tx.clone().unbounded_send(cache.entries()).unwrap();
                    }
                }
            },
        )
        .map(|_| warp::reply::with_status("", warp::http::StatusCode::CREATED));

    let get = warp::get2()
        .and(cache_fut.clone())
        .map(|cache: Arc<Mutex<Cache>>| {
            let cache = cache.clone();
            let cache = cache.lock().unwrap();
            cache.entries()
        })
        .map(|x| warp::reply::json(&x));

    let get_gviz = warp::path("graphviz")
        .and(cache_fut.clone())
        .map(|cache: Arc<Mutex<Cache>>| {
            let cache = cache.clone();
            let cache = cache.lock().unwrap();

            let dag = dag::into_dag(cache.entries()).unwrap();

            format!("{}", Dot::new(&dag))
        });

    let routes = post.or(get_gviz).or(get).with(log);

    let service = warp::serve(routes);

    let (_, fut) = service.bind_ephemeral(([127, 0, 0, 1], port));

    let mut rt = tokio::runtime::Runtime::new().unwrap();

    rt.spawn(fut);

    rt.spawn(
        rx.map_err(|_| -> Error { unreachable!("unbounded rx never errors") })
            .for_each(|x| {
                let now = Instant::now();

                let dag = dag::into_dag(x)?;

                let elapsed = now.elapsed();
                log::debug!(
                    "Built graph in {} ms",
                    (elapsed.as_secs() * 1_000) + u64::from(elapsed.subsec_millis())
                );

                let xs = dag::into_db_records(&dag)?;

                log::debug!("The records I want to insert: {:?}", xs);

                let elapsed = now.elapsed();
                log::debug!(
                    "Built db records in {} ms",
                    (elapsed.as_secs() * 1_000) + u64::from(elapsed.subsec_millis())
                );

                // let conn = connect()?;

                // conn.transaction::<_, diesel::result::Error, _>(|| Ok(()));

                /*
                    Each host contains a subtree of devices. We can use serials to map child devices to any parents across the cluster.
                */

                Ok(())
            })
            .map_err(|e| log::error!("Unhandled Error: {:?}", e)),
    );

    rt.shutdown_on_idle().wait().unwrap();

    Ok(())
}
