// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use device_types::zed::{zfs, zpool, PoolCommand, ZedCommand};
use futures::{Future, Stream};
use std::{error, fmt, io, io::BufReader, num, os::unix::net::UnixStream as NetUnixStream, result};
use tokio::{
    io::lines,
    io::{AsyncRead, AsyncWrite},
    net::UnixStream,
    reactor::Handle,
};

type Result<T> = result::Result<T, Error>;

#[derive(Debug, derive_more::From)]
pub enum Error {
    Io(io::Error),
    SerdeJson(serde_json::Error),
    LibZfsError(libzfs::LibZfsError),
    ParseIntError(num::ParseIntError),
}

impl fmt::Display for Error {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match *self {
            Error::Io(ref err) => write!(f, "{}", err),
            Error::SerdeJson(ref err) => write!(f, "{}", err),
            Error::LibZfsError(ref err) => write!(f, "{}", err),
            Error::ParseIntError(ref err) => write!(f, "{}", err),
        }
    }
}

impl error::Error for Error {
    fn cause(&self) -> Option<&dyn error::Error> {
        match *self {
            Error::Io(ref err) => Some(err),
            Error::SerdeJson(ref err) => Some(err),
            Error::LibZfsError(ref err) => Some(err),
            Error::ParseIntError(ref err) => Some(err),
        }
    }
}

fn guid_to_u64(guid: zpool::Guid) -> Result<u64> {
    let guid: result::Result<u64, std::num::ParseIntError> = guid.into();
    Ok(guid?)
}

/// Takes a ZedCommand and produces some PoolCommands.
pub fn handle_zed_commands(cmd: ZedCommand) -> Result<PoolCommand> {
    log::debug!("Processing ZED event: {:?}", cmd);

    match cmd {
        ZedCommand::Init => {
            let pools = libzfs::get_imported_pools()?;

            Ok(PoolCommand::AddPools(pools))
        }
        ZedCommand::CreateZpool(zpool::Name(name), guid, _) => {
            let guid = guid_to_u64(guid)?;
            let pool = libzfs::get_pool_by_name_and_guid(&name, guid)?;

            Ok(PoolCommand::AddPool(pool))
        }

        ZedCommand::ImportZpool(zpool::Name(name), guid, _)
        | ZedCommand::AddVdev(zpool::Name(name), guid) => {
            let guid = guid_to_u64(guid)?;
            let pool = libzfs::get_pool_by_name_and_guid(&name, guid)?;

            Ok(PoolCommand::UpdatePool(pool))
        }
        ZedCommand::ExportZpool(guid, _) | ZedCommand::DestroyZpool(guid) => {
            Ok(PoolCommand::RemovePool(guid))
        }
        ZedCommand::CreateZfs(guid, zfs::Name(name)) => {
            let dataset = libzfs::get_dataset_by_name(&name)?;

            Ok(PoolCommand::AddDataset(guid, dataset))
        }
        ZedCommand::DestroyZfs(guid, name) => Ok(PoolCommand::RemoveDataset(guid, name)),
        ZedCommand::SetZpoolProp(guid, key, value) => {
            Ok(PoolCommand::SetZpoolProp(guid, key, value))
        }
        ZedCommand::SetZfsProp(guid, name, key, value) => {
            Ok(PoolCommand::SetZfsProp(guid, name, key, value))
        }
    }
}

/// Creates a stream that implements `AsyncWrite`
/// In this case, we use `tokio::net::UnixStream`,
/// But we can substitute this fn for integration testing
pub fn get_write_stream() -> impl AsyncWrite {
    let stream = NetUnixStream::connect("/var/run/device-scanner.sock")
        .expect("Unable to connect to device-scanner.sock");

    UnixStream::from_std(stream, &Handle::default()).expect("Unable to consume device-scanner.sock")
}

pub fn processor(socket: UnixStream) -> impl Future<Item = (), Error = Error> {
    log::trace!("Incoming socket");

    let (r, _) = socket.split();

    lines(BufReader::new(r))
        .map_err(Error::Io)
        .and_then(|x| {
            serde_json::from_str::<device_types::zed::ZedCommand>(&x).map_err(Error::SerdeJson)
        })
        .and_then(handle_zed_commands)
        .map(device_types::Command::PoolCommand)
        .and_then(|x| serde_json::to_string(&x).map_err(Error::SerdeJson))
        .for_each(|x| {
            log::debug!("Sending: {:?}", x);

            tokio::io::write_all(get_write_stream(), x)
                .map(|_| ())
                .map_err(Error::Io)
        })
}
