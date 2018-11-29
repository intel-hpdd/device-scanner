#![allow(proc_macro_derive_resolution_fallback)]

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

pub fn get_connect_string() -> String {
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
