// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use device_types::mount::{Mount, MountCommand};
use im::HashSet;

/// Mutably updates the Mount portion of the device map in response to `MountCommand`s.
pub fn update_mount<S: ::std::hash::BuildHasher>(
    mut local_mounts: HashSet<Mount, S>,
    cmd: MountCommand,
) -> HashSet<Mount, S> {
    match cmd {
        MountCommand::AddMount(target, source, fstype, opts) => {
            local_mounts.update(Mount::new(target, source, fstype, opts))
        }
        MountCommand::RemoveMount(target, source, fstype, opts) => {
            local_mounts.without(&Mount::new(target, source, fstype, opts))
        }
        MountCommand::ReplaceMount(target, source, fstype, opts, old_ops) => {
            local_mounts.remove(&Mount::new(
                target.clone(),
                source.clone(),
                fstype.clone(),
                old_ops,
            ));

            local_mounts.update(Mount::new(target, source, fstype, opts))
        }
        MountCommand::MoveMount(target, source, fstype, opts, old_target) => {
            local_mounts.remove(&Mount::new(
                old_target,
                source.clone(),
                fstype.clone(),
                opts.clone(),
            ));

            local_mounts.update(Mount::new(target, source, fstype, opts))
        }
    }
}

#[cfg(test)]
mod tests {
    use super::update_mount;
    use device_types::{
        mount::{FsType, Mount, MountCommand, MountOpts, MountPoint},
        DevicePath,
    };
    use im::hashset;
    use insta::assert_debug_snapshot_matches;
    use std::path::PathBuf;

    fn create_path_buf(s: &str) -> PathBuf {
        let mut p = PathBuf::new();
        p.push(s);

        p
    }

    #[test]
    fn test_mount_update() {
        let mounts: im::HashSet<device_types::mount::Mount> = hashset!();

        let add_cmd = MountCommand::AddMount(
            MountPoint(create_path_buf("/mnt/part1")),
            DevicePath(create_path_buf("/dev/sde1")),
            FsType("ext4".to_string()),
            MountOpts("rw,relatime,data=ordered".to_string()),
        );

        let mounts = update_mount(mounts, add_cmd);

        assert_debug_snapshot_matches!(mounts);

        let mv_cmd = MountCommand::MoveMount(
            MountPoint(create_path_buf("/mnt/part3")),
            DevicePath(create_path_buf("/dev/sde1")),
            FsType("ext4".to_string()),
            MountOpts("rw,relatime,data=ordered".to_string()),
            MountPoint(create_path_buf("/mnt/part1")),
        );

        let mounts = update_mount(mounts, mv_cmd);

        assert_eq!(
            hashset!(Mount {
                target: MountPoint(create_path_buf("/mnt/part3")),
                source: DevicePath(create_path_buf("/dev/sde1")),
                fs_type: FsType("ext4".to_string()),
                opts: MountOpts("rw,relatime,data=ordered".to_string())
            }),
            mounts
        );

        let replace_cmd = MountCommand::ReplaceMount(
            MountPoint(create_path_buf("/mnt/part3")),
            DevicePath(create_path_buf("/dev/sde1")),
            FsType("ext4".to_string()),
            MountOpts("r,relatime,data=ordered".to_string()),
            MountOpts("rw,relatime,data=ordered".to_string()),
        );

        let mounts = update_mount(mounts, replace_cmd);

        assert_eq!(
            hashset!(Mount {
                target: MountPoint(create_path_buf("/mnt/part3")),
                source: DevicePath(create_path_buf("/dev/sde1")),
                fs_type: FsType("ext4".to_string()),
                opts: MountOpts("r,relatime,data=ordered".to_string())
            }),
            mounts
        );

        let rm_cmd = MountCommand::RemoveMount(
            MountPoint(create_path_buf("/mnt/part3")),
            DevicePath(create_path_buf("/dev/sde1")),
            FsType("ext4".to_string()),
            MountOpts("r,relatime,data=ordered".to_string()),
        );

        let mounts = update_mount(mounts, rm_cmd);
        assert_eq!(hashset!(), mounts);
    }
}
