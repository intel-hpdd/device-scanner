// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use device_aggregator::{aggregator_error, dag, db};
use device_types::devices;

struct DevGraph(pub dag::Dag);

impl DevGraph {
    fn new() -> Self {
        DevGraph(daggy::Dag::new())
    }
    fn create_host(self: &mut Self, s: &str) -> (devices::Host, daggy::NodeIndex) {
        let device = devices::Host(s.to_string());
        let nx = self.0.add_node(devices::Device::Host(device.clone()));

        (device, nx)
    }
    fn add_shared(
        &mut self,
        a: daggy::NodeIndex,
        b: daggy::NodeIndex,
    ) -> aggregator_error::Result<daggy::EdgeIndex> {
        self.0
            .add_edge(a, b, dag::Edge::Shared)
            .map_err(aggregator_error::Error::WouldCycle)
    }
}

trait AddChild<T> {
    fn add_child(
        &mut self,
        x: T,
        device: devices::Device,
    ) -> aggregator_error::Result<daggy::NodeIndex>;
}

impl AddChild<daggy::NodeIndex> for DevGraph {
    fn add_child(
        &mut self,
        x: daggy::NodeIndex,
        device: devices::Device,
    ) -> aggregator_error::Result<daggy::NodeIndex> {
        let (_, nx) = self.0.add_child(x, dag::Edge::Parent, device);

        Ok(nx)
    }
}

impl AddChild<Vec<daggy::NodeIndex>> for DevGraph {
    fn add_child(
        &mut self,
        xs: Vec<daggy::NodeIndex>,
        device: devices::Device,
    ) -> aggregator_error::Result<daggy::NodeIndex> {
        let n = self.0.add_node(device);

        for x in xs {
            self.0.add_edge(x, n, dag::Edge::Parent)?;
        }

        Ok(n)
    }
}

impl From<DevGraph> for dag::Dag {
    fn from(d: DevGraph) -> Self {
        d.0
    }
}

#[test]
fn test_get_distinct_hosts() -> aggregator_error::Result<()> {
    let mut dag = DevGraph::new();

    let (device1, host1) = dag.create_host("host1");
    let (device2, host2) = dag.create_host("host2");

    let scsi1 = dag.add_child(host1, devices::Device::ScsiDevice(Default::default()))?;

    let scsi2 = dag.add_child(host2, devices::Device::ScsiDevice(Default::default()))?;

    dag.add_shared(scsi1, scsi2)?;

    let actual = dag::get_distinct_hosts(&dag.0, scsi1)?;

    assert_eq!(im::hashset![&device1, &device2], actual);

    Ok(())
}

#[test]
fn test_get_distinct_hosts_one_host() -> aggregator_error::Result<()> {
    let mut dag = DevGraph::new();

    let (_, host1) = dag.create_host("host1");
    let (device2, host2) = dag.create_host("host2");

    let scsi1 = dag.add_child(host1, devices::Device::ScsiDevice(Default::default()))?;
    let scsi2 = dag.add_child(host2, devices::Device::ScsiDevice(Default::default()))?;
    dag.add_shared(scsi1, scsi2)?;

    let scsi3 = dag.add_child(host2, devices::Device::ScsiDevice(Default::default()))?;

    let mpath1 = dag.add_child(scsi1, devices::Device::Mpath(Default::default()))?;
    let mpath2 = dag.add_child(scsi2, devices::Device::Mpath(Default::default()))?;
    dag.add_shared(mpath1, mpath2)?;

    let partition = dag.add_child(scsi3, devices::Device::Partition(Default::default()))?;

    let vg = dag.add_child(
        vec![mpath2, partition],
        devices::Device::VolumeGroup(Default::default()),
    )?;

    let lv = dag.add_child(vg, devices::Device::LogicalVolume(Default::default()))?;

    let actual = dag::get_distinct_hosts(&dag.0, lv)?;

    assert_eq!(im::hashset![&device2], actual);

    Ok(())
}

#[test]
fn test_into_device_set() -> aggregator_error::Result<()> {
    let mut dag = DevGraph::new();

    let (_, host1) = dag.create_host("host1");
    let (_, host2) = dag.create_host("host2");

    let scsi1 = dag.add_child(host1, devices::Device::ScsiDevice(Default::default()))?;
    let scsi2 = dag.add_child(host2, devices::Device::ScsiDevice(Default::default()))?;
    dag.add_shared(scsi1, scsi2)?;

    let scsi3 = dag.add_child(host2, devices::Device::ScsiDevice(Default::default()))?;

    let mpath1 = dag.add_child(scsi1, devices::Device::Mpath(Default::default()))?;
    let mpath2 = dag.add_child(scsi2, devices::Device::Mpath(Default::default()))?;
    dag.add_shared(mpath1, mpath2)?;

    let partition = dag.add_child(scsi3, devices::Device::Partition(Default::default()))?;

    let vg = dag.add_child(
        vec![mpath2, partition],
        devices::Device::VolumeGroup(Default::default()),
    )?;

    let lv_device = devices::Device::LogicalVolume(Default::default());
    dag.add_child(vg, lv_device.clone())?;

    let inner = &dag.into();

    let result = dag::into_db_records(&inner)?;

    assert_eq!(
        im::ordset![(
            im::ordset![db::DeviceHost {
                device_type: "logical volume".to_string(),
                device_serial: "".to_string(),
                host_fqdn: "host2".to_string(),
                paths: vec![],
                mount_path: None,
                is_active: true
            }],
            db::Device {
                device_type: "logical volume".to_string(),
                serial: "".to_string(),
                size: 0,
                fs_type: None
            }
        )],
        result
    );

    Ok(())
}
