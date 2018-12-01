use aggregator_error;
use device_types::devices;
use diesel::{pg::PgConnection, prelude::*};
use env::get_var;

#[derive(Queryable)]
pub struct Device {
    pub id: i32,
    pub size: u64,
    pub device_type: String,
    pub serial: String,
    pub fs_type: Option<String>,
    pub mount_path: Option<String>,
}

#[derive(Queryable)]
pub struct DeviceHost {
    pub id: i32,
    pub device_id: i32,
    pub paths: Vec<String>,
    pub host_fqdn: String,
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

// Walk from the given node back to the roots.
//
// The intersection of all the root hosts is where the device can be mounted.
// fn get_distinct_parents(dag: &dag::Dag, n: daggy::NodeIndex) -> im::HashSet<&devices::Host> {
//     let parents = dag.parents(n);

//     parents
//         .iter(dag)
//         .map(|(_, p)| match dag.node_weight(p).unwrap() {
//             devices::Device::Host(h) => h,
//             _ => get_distinct_parents(dag, n),
//         }).collect()
// }

// pub fn populate_db(dag: &dag::Dag) {
//     for (n, d) in dag.node_references() {
//         match d {
//             devices::Device::Host(_) => {}
//             devices::Device::Mpath(m) => if let Some(ref fs) = m.filesystem_type {},
//             devices::Device::ScsiDevice(s) => {}
//             devices::Device::Partition(p) => {}
//             devices::Device::VolumeGroup(vg) => {}
//             devices::Device::LogicalVolume(lv) => {}
//             devices::Device::MdRaid(m) => {}
//             devices::Device::Zpool(z) => {}
//             devices::Device::Dataset(_) => {}
//         }
//     }
// }
