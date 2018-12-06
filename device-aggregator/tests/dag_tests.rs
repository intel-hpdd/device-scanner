extern crate daggy;
extern crate device_aggregator;
extern crate device_types;
extern crate im;

use daggy::{Dag, Walker};

use device_types::devices;

use device_aggregator::dag;
use std::path::PathBuf;

#[test]
fn test_get_distinct_hosts() {
    let mut dag = daggy::Dag::new();

    let device1 = devices::Host("host1".to_string());
    let host1 = dag.add_node(devices::Device::Host(device1.clone()));
    let device2 = devices::Host("host1".to_string());
    let host2 = dag.add_node(devices::Device::Host(device2.clone()));

    let (_, scsi1) = dag.add_child(
        host1,
        dag::Edge::Parent,
        devices::Device::ScsiDevice(devices::ScsiDevice {
            serial: devices::Serial("3600140547abdb28e0d74699961a09c99".to_string()),
            major: "8".to_string(),
            minor: "32".to_string(),
            devpath: "/devices/platform/host3/session2/target3:0:0/3:0:0:0/block/sdc".into(),
            size: 104857600,
            filesystem_type: Some("mpath_member".to_string()),
            paths: im::hashset![
            "/dev/disk/by-label/test".into(),
            "/dev/disk/by-id/wwn-0x600140547abdb28e0d74699961a09c99".into(),
            "/dev/disk/by-uuid/8981758005725934928".into(),
            "/dev/sdc".into(),
            "/dev/disk/by-path/ip-10.0.50.10:3260-iscsi-iqn.2015-01.com.whamcloud.lu:disks-lun-0".into(),
            "/dev/disk/by-id/scsi-3600140547abdb28e0d74699961a09c99".into()
            ],
            mount_path: None,
        }),
    );

    let (_, scsi2) = dag.add_child(
        host2, 
        dag::Edge::Parent, 
        devices::Device::ScsiDevice(devices::ScsiDevice { 
            serial: devices::Serial("3600140547abdb28e0d74699961a09c99".to_string()), 
            major: "8".to_string(), 
            minor: "16".to_string(),
            devpath: "/devices/platform/host2/session1/target2:0:0/2:0:0:0/block/sdb".into(), 
            size: 104857600, 
            filesystem_type: Some("mpath_member".to_string()), 
            paths: im::hashset![
                "/dev/disk/by-id/scsi-3600140547abdb28e0d74699961a09c99".into(), 
                "/dev/disk/by-uuid/8981758005725934928".into(), 
                "/dev/disk/by-label/test".into(), 
                "/dev/disk/by-id/wwn-0x600140547abdb28e0d74699961a09c99".into(), 
                "/dev/sdb".into(), 
                "/dev/disk/by-path/ip-10.0.40.10:3260-iscsi-iqn.2015-01.com.whamcloud.lu:disks-lun-0".into()
            ], 
            mount_path: None
        })
    );

    dag.add_edge(scsi1, scsi2, dag::Edge::Shared).unwrap();

    let actual = dag::get_distinct_hosts(&dag, scsi1);

    assert_eq!(im::hashset![device1, device2], actual);
}
