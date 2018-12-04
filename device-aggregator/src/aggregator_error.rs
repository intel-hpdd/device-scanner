// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use dag::Edge;
use daggy::WouldCycle;
use serde_json;
use std::io;
use std::{error, result};
use tokio::timer;

pub type Result<T> = result::Result<T, Error>;

#[derive(Debug, derive_more::Display, derive_more::From)]
pub enum Error {
    Io(io::Error),
    Timer(timer::Error),
    SerdeJsonError(serde_json::Error),
    ConnectionError(diesel::ConnectionError),
    WouldCycle(WouldCycle<Edge>),
}

impl error::Error for Error {
    fn cause(&self) -> Option<&error::Error> {
        match *self {
            Error::Io(ref err) => Some(err),
            Error::Timer(ref err) => Some(err),
            Error::SerdeJsonError(ref err) => Some(err),
            Error::ConnectionError(ref err) => Some(err),
            Error::WouldCycle(ref err) => Some(err),
        }
    }
}
