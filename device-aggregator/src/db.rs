// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

//! # DB
//!
//! This module is an integration point between device-aggregator and chroma_core.
//!
//! It populates two tables, `device` and `device_host`.
//!
//! A `device` is a generic device that may be present on multiple hosts.
//!
//! A `device_host` is the instance of that device on a host.
//!
//! `device` to `device_host` is a 1:M relationship.
//!
//! Up to this point we have been stateless, but this module is stateful by using the db.
//!
//! The choice to be stateful here is because:
//!
//! 1. The need to have a "balanced" set of Lustre targets, and for this balance to be stable across ticks of
//!    device discovery.
//! 2. The need for devices to not be forgotten if they are in use as Lustre targets. Instead this should be an alert.
//!

use std::path::PathBuf;

use diesel::{pg::PgConnection, prelude::*};

use device_types::devices;

use crate::{
    aggregator_error, dag,
    env::get_var,
    schema::{device, device_host},
};


#[table_name = "device"]
#[derive(Insertable, Queryable, Debug, PartialEq, Eq, Hash, Clone)]
pub struct Device {
    pub device_type: String,
    pub serial: String,
    pub size: i64,
    pub fs_type: Option<String>,
}

impl Device {
    pub fn new(
        size: i64,
        device_type: &devices::DeviceType,
        serial: &devices::Serial,
        fs_type: &Option<String>,
    ) -> Self {
        Device {
            size,
            device_type: device_type.to_string(),
            serial: serial.to_string(),
            fs_type: fs_type.clone(),
        }
    }
}

#[table_name = "device_host"]
#[derive(Queryable, Insertable, Debug, PartialEq, Eq, Hash, Clone)]
pub struct DeviceHost {
    pub device_type: String,
    pub device_serial: String,
    pub host_fqdn: String,
    pub paths: Vec<String>,
    pub mount_path: Option<String>,
    pub is_active: bool,
}

impl DeviceHost {
    pub fn new(
        paths: &devices::Paths,
        devices::Host(host_fqdn): &devices::Host,
        device_type: &devices::DeviceType,
        device_serial: &devices::Serial,
        mount_path: &devices::MountPath,
        is_active: bool,
    ) -> Self {
        fn pathbuf_to_string(p: &PathBuf) -> String {
            p.to_string_lossy().to_string()
        }

        let paths = paths.into_iter().map(pathbuf_to_string).collect();

        let mount_path = mount_path.as_ref().map(|p| pathbuf_to_string(p));

        DeviceHost {
            device_type: device_type.to_string(),
            device_serial: device_serial.to_string(),
            host_fqdn: host_fqdn.to_string(),
            paths,
            mount_path,
            is_active,
        }
    }
}

pub fn create_records_from_device_and_hosts<'a>(
    (d, hs): (&'a devices::Device, im::HashSet<dag::Host<'a>>),
) -> aggregator_error::Result<(im::HashSet<DeviceHost>, Device)> {
    log::debug!("create_records_from_device_and_hosts");

    let d = d.as_mountable_storage_device().ok_or_else(|| {
        aggregator_error::Error::graph_error(format!(
            "Could not convert {:?} to mountable storage device",
            d
        ))
    });

    let d = d.map(|d| {
        let dev = Device::new(d.size(), &d.name(), &d.serial(), &d.filesystem_type());

        let dev_hosts = hs
            .into_iter()
            .map(|host| match host {
                dag::Host::Active(host) => DeviceHost::new(
                    &d.paths(),
                    &host,
                    &d.name(),
                    &d.serial(),
                    &d.mount_path(),
                    true,
                ),
                dag::Host::Inactive(host) => DeviceHost::new(
                    &d.paths(),
                    &host,
                    &d.name(),
                    &d.serial(),
                    &d.mount_path(),
                    false,
                ),
            })
            .collect();

        log::debug!("returning: ({:?}, {:?})", dev_hosts, dev);

        (dev_hosts, dev)
    });

    log::debug!("Exiting: create_records_from_device_and_hosts");

    d
}

fn get_connect_string() -> String {
    let db_host = get_var("DB_HOST");
    let db_name = get_var("DB_NAME");
    let db_user = get_var("DB_USER");
    let db_password = get_var("DB_PASSWORD");

    let db_password = match db_password.as_ref() {
        "" => db_password,
        _ => format!(":{}", db_password),
    };

    format!(
        "postgresql://{}{}@{}/{}",
        db_user, db_password, db_host, db_name
    )
}

pub fn connector() -> impl Fn() -> aggregator_error::Result<diesel::PgConnection> {
    let connect_string = get_connect_string();

    move || {
        PgConnection::establish(&connect_string.as_str())
            .map_err(aggregator_error::Error::ConnectionError)
    }
}
