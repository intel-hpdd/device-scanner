// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use device_types::zed::{zfs, zpool, PoolCommand, ZedCommand};
use futures::{Future, Stream, StreamExt, TryStreamExt};
use std::{error, fmt, io, io::BufReader, num, os::unix::net::UnixStream as NetUnixStream, result};
use tokio::{
    codec::{FramedRead, LinesCodec, LinesCodecError},
    io::{AsyncRead, AsyncWrite, AsyncWriteExt},
    net::UnixStream,
};

type Result<T> = result::Result<T, Error>;

#[derive(Debug, derive_more::From)]
pub enum Error {
    Io(io::Error),
    SerdeJson(serde_json::Error),
    LibZfsError(libzfs::LibZfsError),
    ParseIntError(num::ParseIntError),
    LinesCodecError(LinesCodecError),
}

impl fmt::Display for Error {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match *self {
            Error::Io(ref err) => write!(f, "{}", err),
            Error::SerdeJson(ref err) => write!(f, "{}", err),
            Error::LibZfsError(ref err) => write!(f, "{}", err),
            Error::ParseIntError(ref err) => write!(f, "{}", err),
            Error::LinesCodecError(ref err) => write!(f, "{}", err),
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
            Error::LinesCodecError(ref err) => Some(err),
        }
    }
}

fn guid_to_u64(guid: zpool::Guid) -> Result<u64> {
    let guid: result::Result<u64, std::num::ParseIntError> = guid.into();
    Ok(guid?)
}

/// Takes a ZedCommand and produces some PoolCommands.
pub fn handle_zed_commands(cmd: ZedCommand) -> Result<PoolCommand> {
    tracing::debug!("Processing ZED event: {:?}", cmd);

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
pub async fn get_write_stream() -> Result<impl AsyncWrite> {
    Ok(UnixStream::connect("/var/run/device-scanner.sock").await?)
}

pub async fn processor(socket: UnixStream) -> Result<()> {
    tracing::trace!("Incoming socket");

    let mut line_stream = FramedRead::new(socket, LinesCodec::new());
    while let Some(line) = line_stream.try_next().await? {
        let zed_command = serde_json::from_str::<device_types::zed::ZedCommand>(&line)?;
        let pool_command = handle_zed_commands(zed_command)?;
        let pool_command_str = serde_json::to_string(&pool_command)?;

        tracing::debug!("Sending: {:?}", pool_command_str);

        get_write_stream().await?.write_all(pool_command_str.as_bytes()).await?;
    }

    Ok(())
}
