// Copyright (c) 2019 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use crate::aggregator_error;
use device_types::devices::Device;
use futures::{try_ready, Async, Future, Poll, Stream};
use std::{
    collections::HashMap,
    sync::{Arc, Mutex},
    time::Duration,
};
use tokio::timer::{delay_queue, DelayQueue};

const TTL_SECS: u64 = 30;

/// Holds an in-memory cache of `Device`
///
/// The `Cache` will expire it's entries
/// after `TTL_SECS` have expired
pub struct Cache {
    entries: HashMap<String, (delay_queue::Key, Device)>,
    pub expirations: DelayQueue<String>,
}

impl Default for Cache {
    fn default() -> Self {
        Cache {
            entries: HashMap::new(),
            expirations: DelayQueue::new(),
        }
    }
}

impl Cache {
    /// Inserts some `Device` into the cache for
    /// the given host
    ///
    /// The `Device` will expire from the `Cache` once
    /// `TTL_SECS` has elapsed
    fn insert(&mut self, host: &str, state: Device) {
        let key = self
            .expirations
            .insert(host.to_string(), Duration::from_secs(TTL_SECS));

        self.entries.insert(host.to_string(), (key, state));
    }
    /// Inserts or updates state in the `Cache`
    ///
    /// If the key exists in the `Cache`
    /// it's expiration is reset and it's value is updated
    ///
    /// If the key does not exist, it is added to the `Cache` with
    /// the default TTL.
    pub fn upsert(&mut self, host: &str, state: Device) {
        match self.entries.remove(host) {
            Some((k, _)) => {
                self.expirations.reset(&k, Duration::from_secs(TTL_SECS));
                self.entries.insert(host.to_string(), (k, state));
            }
            None => self.insert(host, state),
        };
    }
    pub fn entries(&self) -> HashMap<String, Device> {
        self.entries
            .iter()
            .map(|(host, (_, ref state))| (host.clone(), state.clone()))
            .collect()
    }
    /// Resets the host item TTL
    pub fn reset(&mut self, host: &str) {
        if let Some((key, _)) = self.entries.get(host) {
            self.expirations.reset(key, Duration::from_secs(TTL_SECS));
        }
    }
    fn poll_purge(&mut self) -> Poll<(), aggregator_error::Error> {
        while let Some(entry) = try_ready!(self.expirations.poll()) {
            self.entries.remove(entry.get_ref());
        }

        Ok(Async::Ready(()))
    }
}

/// Wraps the `Cache` and polls it till all
/// expired items have been flushed
pub struct CacheFlush(pub Option<Arc<Mutex<Cache>>>);

impl CacheFlush {
    pub fn new(cache: Arc<Mutex<Cache>>) -> Self {
        CacheFlush(Some(cache))
    }
}

impl Future for CacheFlush {
    type Item = Arc<Mutex<Cache>>;
    type Error = aggregator_error::Error;

    fn poll(&mut self) -> Result<Async<Self::Item>, Self::Error> {
        let cache = self.0.take().unwrap();

        cache.lock().unwrap().poll_purge()?;

        Ok(Async::Ready(cache))
    }
}
