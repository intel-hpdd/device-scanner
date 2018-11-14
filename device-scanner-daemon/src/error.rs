// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use futures::sync::mpsc;
use serde_json;
use std::{error, fmt, io, num, result};

pub type Result<T> = result::Result<T, Error>;

pub fn none_error<E>(error: E) -> Error
where
    E: Into<Box<dyn error::Error + Send + Sync>>,
{
    Error::NoneError(error.into())
}

#[derive(Debug)]
pub enum Error {
    Io(io::Error),
    SendError(Box<dyn error::Error + Send + Sync>),
    SerdeJson(serde_json::Error),
    LibZfsError(libzfs_types::LibZfsError),
    ParseIntError(num::ParseIntError),
    NoneError(Box<dyn error::Error + Send + Sync>),
}

impl fmt::Display for Error {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match *self {
            Error::Io(ref err) => write!(f, "{}", err),
            Error::SendError(ref err) => write!(f, "{}", err),
            Error::SerdeJson(ref err) => write!(f, "{}", err),
            Error::LibZfsError(ref err) => write!(f, "{}", err),
            Error::ParseIntError(ref err) => write!(f, "{}", err),
            Error::NoneError(ref err) => write!(f, "{}", err),
        }
    }
}

impl error::Error for Error {
    fn cause(&self) -> Option<&error::Error> {
        match *self {
            Error::Io(ref err) => Some(err),
            Error::SendError(_) => None,
            Error::SerdeJson(ref err) => Some(err),
            Error::LibZfsError(ref err) => Some(err),
            Error::ParseIntError(ref err) => Some(err),
            Error::NoneError(_) => None,
        }
    }
}

impl From<io::Error> for Error {
    fn from(err: io::Error) -> Self {
        Error::Io(err)
    }
}

impl<E> From<mpsc::SendError<E>> for Error
where
    E: Send + Sync + 'static,
{
    fn from(err: mpsc::SendError<E>) -> Self {
        Error::SendError(Box::new(err))
    }
}

impl From<serde_json::Error> for Error {
    fn from(err: serde_json::Error) -> Self {
        Error::SerdeJson(err)
    }
}

impl From<libzfs_types::LibZfsError> for Error {
    fn from(err: libzfs_types::LibZfsError) -> Self {
        Error::LibZfsError(err)
    }
}

impl From<num::ParseIntError> for Error {
    fn from(err: num::ParseIntError) -> Self {
        Error::ParseIntError(err)
    }
}
