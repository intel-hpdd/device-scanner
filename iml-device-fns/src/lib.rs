extern crate im;
extern crate libzfs_types;

use std::path::PathBuf;

use im::{hashset, HashSet};

/// Traverses a VDev tree and returns back it's paths
pub fn get_vdev_paths(vdev: libzfs_types::VDev) -> HashSet<PathBuf> {
    match vdev {
        libzfs_types::VDev::Disk { path, .. } => hashset![path],
        libzfs_types::VDev::File { .. } => hashset![],
        libzfs_types::VDev::Mirror { children, .. }
        | libzfs_types::VDev::RaidZ { children, .. }
        | libzfs_types::VDev::Replacing { children, .. } => {
            children.into_iter().flat_map(get_vdev_paths).collect()
        }
        libzfs_types::VDev::Root {
            children,
            spares,
            cache,
            ..
        } => vec![children, spares, cache]
            .into_iter()
            .flatten()
            .flat_map(get_vdev_paths)
            .collect(),
    }
}

pub fn format_major_minor(major: &str, minor: &str) -> String {
    format!("{}:{}", major, minor)
}

pub fn find_by_major_minor(xs: &im::Vector<String>, major: &str, minor: &str) -> bool {
    xs.contains(&format_major_minor(major, minor))
}
