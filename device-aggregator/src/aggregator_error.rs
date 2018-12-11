// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use crate::dag::Edge;
use daggy::WouldCycle;
use serde_json;
use std::fmt::Display;
use std::{error, fmt, io, result};
use tokio::timer;

pub type Result<T> = result::Result<T, Error>;

#[derive(Debug, derive_more::From)]
pub enum Error {
    Io(io::Error),
    Timer(timer::Error),
    SerdeJsonError(serde_json::Error),
    ConnectionError(diesel::ConnectionError),
    WouldCycle(WouldCycle<Edge>),
    GraphError(String),
}

impl Error {
    pub fn graph_error(s: impl Display) -> Self {
        Error::GraphError(s.to_string())
    }
}

impl fmt::Display for Error {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match *self {
            Error::Io(ref err) => write!(f, "{}", err),
            Error::Timer(ref err) => write!(f, "{}", err),
            Error::SerdeJsonError(ref err) => write!(f, "{}", err),
            Error::ConnectionError(ref err) => write!(f, "{}", err),
            Error::WouldCycle(ref err) => write!(f, "{}", err),
            Error::GraphError(ref s) => write!(
                f,
                "There was an error working with device-aggregator graph: {}",
                s
            ),
        }
    }
}

impl error::Error for Error {
    fn cause(&self) -> Option<&error::Error> {
        match *self {
            Error::Io(ref err) => Some(err),
            Error::Timer(ref err) => Some(err),
            Error::SerdeJsonError(ref err) => Some(err),
            Error::ConnectionError(ref err) => Some(err),
            Error::WouldCycle(ref err) => Some(err),
            Error::GraphError(_) => None,
        }
    }
}
