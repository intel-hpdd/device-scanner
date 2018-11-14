// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

extern crate device_types;
extern crate futures;
extern crate serde;
extern crate serde_json;
extern crate tokio;

pub mod aggregator_error {
    use serde_json;
    use std::io;
    use std::{error, fmt, result, str};
    use tokio::timer;

    pub type Result<T> = result::Result<T, Error>;

    #[derive(Debug)]
    pub enum Error {
        Io(io::Error),
        Timer(timer::Error),
        SerdeJsonError(serde_json::Error),
    }

    impl fmt::Display for Error {
        fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
            match *self {
                Error::Io(ref err) => write!(f, "{}", err),
                Error::Timer(ref err) => write!(f, "{}", err),
                Error::SerdeJsonError(ref err) => write!(f, "{}", err),
            }
        }
    }

    impl error::Error for Error {
        fn description(&self) -> &str {
            match *self {
                Error::Io(ref err) => err.description(),
                Error::Timer(ref err) => err.description(),
                Error::SerdeJsonError(ref err) => err.description(),
            }
        }

        fn cause(&self) -> Option<&error::Error> {
            match *self {
                Error::Io(ref err) => Some(err),
                Error::Timer(ref err) => Some(err),
                Error::SerdeJsonError(ref err) => Some(err),
            }
        }
    }

    impl From<io::Error> for Error {
        fn from(err: io::Error) -> Self {
            Error::Io(err)
        }
    }

    impl From<timer::Error> for Error {
        fn from(err: timer::Error) -> Self {
            Error::Timer(err)
        }
    }

    impl From<serde_json::Error> for Error {
        fn from(err: serde_json::Error) -> Self {
            Error::SerdeJsonError(err)
        }
    }
}

pub mod cache {
    use super::aggregator_error;

    use std::{
        collections::HashMap,
        sync::{Arc, Mutex},
        time::Duration,
    };

    use futures::{try_ready, Async, Future, Poll, Stream};
    use tokio::timer::{delay_queue, DelayQueue};

    use device_types::devices::Device;

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
}
