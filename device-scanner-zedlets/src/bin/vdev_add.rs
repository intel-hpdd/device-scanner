extern crate device_scanner_zedlets;
extern crate device_types;

use device_scanner_zedlets::{send_data, zpool, Result};
use device_types::zed::ZedCommand;

fn main() -> Result<()> {
    let x = ZedCommand::AddVdev(zpool::get_name()?, zpool::get_guid()?);

    send_data(x)
}
