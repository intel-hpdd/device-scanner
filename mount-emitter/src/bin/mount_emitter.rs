#![allow(unknown_lints)]
#![warn(clippy)]

extern crate mount_emitter;
extern crate tokio;

use mount_emitter::{get_write_stream, looper, stdin_to_file, write_all};

fn main() {
    tokio::run(looper(stdin_to_file, get_write_stream, write_all))
}
