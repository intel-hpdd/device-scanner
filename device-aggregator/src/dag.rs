// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use std::iter::once;

use im::{hashset, HashSet};

use daggy::petgraph::{
    self, graph,
    visit::{Dfs, EdgeRef, IntoNodeReferences},
};

use device_types::devices::{self, AsParent, Device, Parent};

use crate::{
    aggregator_error::{Error, Result},
    db,
};

#[derive(Debug, Clone, Hash, Eq, PartialEq)]
pub enum Host<'a> {
    Active(&'a devices::Host),
    Inactive(&'a devices::Host),
}

/// Devices can be connected to
/// each other by a parent -> child relationship
/// or a shared device <-> device relationship.
#[derive(Copy, Clone, Debug, Hash, Eq, PartialEq, derive_more::Display)]
pub enum Edge {
    Parent,
    Shared,
}

impl Edge {
    fn is_shared(self) -> bool {
        match self {
            Edge::Shared => true,
            _ => false,
        }
    }
    fn is_parent(self) -> bool {
        match self {
            Edge::Parent => true,
            _ => false,
        }
    }
}

pub type Dag = daggy::Dag<Device, Edge>;

/// Higher-order function that filters a dag based on the provided predicate.
pub fn filter_refs(dag: &Dag, f: impl Fn(&Device) -> bool) -> Vec<daggy::NodeIndex> {
    dag.node_references()
        .filter(move |(_, x)| f(x))
        .map(|(x, _)| x)
        .collect()
}

/// Is the device a scsi device
fn is_scsi(d: &Device) -> bool {
    match d {
        Device::ScsiDevice(_) => true,
        _ => false,
    }
}

/// Is the device a partition
fn is_partition(d: &Device, parent: &Parent) -> bool {
    match d {
        Device::Partition(p) => p.parents.contains(parent),
        _ => false,
    }
}

/// Is the device a volume group
fn is_vg(d: &Device, parent: &Parent) -> bool {
    match d {
        Device::VolumeGroup(vg) => vg.parents.contains(parent),
        _ => false,
    }
}

/// Is the device a logical volume
fn is_lv(d: &Device, parent: &Parent) -> bool {
    match d {
        Device::LogicalVolume(lv) => parent == &lv.parent,
        _ => false,
    }
}

/// Is the device a multipath device
fn is_mpath(d: &Device, parent: &Parent) -> bool {
    match d {
        Device::Mpath(m) => m.parents.contains(parent),
        _ => false,
    }
}

/// Is the device a mdraid device
fn is_md(d: &Device, parent: &Parent) -> bool {
    match d {
        Device::MdRaid(m) => m.parents.contains(parent),
        _ => false,
    }
}

/// Is the device a zpool
fn is_pool(d: &Device, parent: &Parent) -> bool {
    match d {
        Device::Zpool(z) => z.parents.contains(parent),
        _ => false,
    }
}

/// Is the device a dataset
fn is_dataset(d: &Device, serial: &devices::Serial) -> bool {
    match d {
        Device::Dataset(d) => &d.pool_serial == serial,
        _ => false,
    }
}

/// Add the subtree rooted at root of the rhs to the lhs.
pub fn add(l: &mut Dag, r: &Dag, root: daggy::NodeIndex) -> Result<()> {
    let mut mapping = im::HashMap::new();

    let graph = r.graph();

    for (old_idx, d) in graph.node_references() {
        mapping.insert(old_idx, l.add_node(d.clone()));
    }

    let mut dfs = Dfs::new(&graph, root);

    while let Some(nx) = dfs.next(&graph) {
        for nx2 in graph.neighbors_directed(nx, petgraph::Incoming) {
            l.update_edge(mapping[&nx2], mapping[&nx], Edge::Parent)?;
        }
    }

    Ok(())
}

/// Given a dag and a starting node, return an iterator of the node's
/// parents.
fn get_parents<'a>(
    dag: &'a Dag,
    nx: daggy::NodeIndex,
) -> impl Iterator<Item = daggy::NodeIndex> + 'a {
    dag.graph()
        .edges_directed(nx, petgraph::Incoming)
        .filter(|e| e.weight().is_parent())
        .map(|e| e.source())
}

/// Given a dag and a starting node, return an iterator of the node's
/// children.
fn get_children<'a>(
    dag: &'a Dag,
    nx: daggy::NodeIndex,
) -> impl Iterator<Item = daggy::NodeIndex> + 'a {
    dag.graph()
        .edges_directed(nx, petgraph::Outgoing)
        .filter(|e| e.weight().is_parent())
        .map(|e| e.target())
}

/// Given a dag and a search node, find it's shared siblings.
fn get_shared<'a>(
    dag: &'a Dag,
    nx: daggy::NodeIndex,
) -> impl Iterator<Item = graph::NodeIndex> + 'a {
    let graph = dag.graph();

    graph
        .edges_directed(nx, petgraph::Outgoing)
        .chain(graph.edges_directed(nx, petgraph::Incoming))
        .filter(|e| e.weight().is_shared())
        .filter_map(move |e| match (e.source(), e.target()) {
            (source, target) if source == nx => Some(target),
            (source, target) if target == nx => Some(source),
            _ => None,
        })
}

fn get_shared_and_self<'a>(
    dag: &'a Dag,
    nx: daggy::NodeIndex,
) -> impl Iterator<Item = graph::NodeIndex> + 'a {
    self::get_shared(dag, nx).chain(once(nx))
}

fn get_all_node_children<'a>(
    dag: &'a Dag,
    nx: daggy::NodeIndex,
) -> impl Iterator<Item = graph::NodeIndex> + 'a {
    self::get_shared_and_self(dag, nx).flat_map(move |x| self::get_children(dag, x))
}

fn get_all_node_parents<'a>(
    dag: &'a Dag,
    nx: daggy::NodeIndex,
) -> impl Iterator<Item = graph::NodeIndex> + 'a {
    self::get_shared_and_self(dag, nx).flat_map(move |x| self::get_parents(dag, x))
}

/// Given a dag and a starting node, walk up the parents
/// until the hosts are hit.
pub fn get_active_hosts(dag: &Dag, nx: daggy::NodeIndex) -> Result<im::HashSet<&devices::Host>> {
    let mut nodes = vec![nx];

    let mut hosts = im::hashset![];

    while let Some(nx) = nodes.pop() {
        match self::get_device(dag, nx)? {
            Device::Host(host) => {
                hosts.insert(host);
            }
            _ => nodes.extend(self::get_parents(dag, nx)),
        }
    }

    Ok(hosts)
}

fn get_hosts_from_scsi(dag: &Dag, nx: daggy::NodeIndex) -> Result<im::HashSet<&devices::Host>> {
    self::get_parents(dag, nx)
        .map(|nx| match dag.node_weight(nx) {
            Some(devices::Device::Host(h)) => Ok(h),
            _ => Err(Error::graph_error("Could not find host parent from device")),
        })
        .collect()
}

fn get_device(dag: &Dag, nx: daggy::NodeIndex) -> Result<&devices::Device> {
    match dag.node_weight(nx) {
        Some(x) => Ok(x),
        None => Err(Error::graph_error("Did find node_weight"))?,
    }
}

/// Given a dag and a starting leaf device, walk up towards the root
/// collecting the set of hosts that can use this device.
/// The resulting set is an intersection of all hosts that can use the device.
pub fn get_distinct_hosts(dag: &Dag, nx: daggy::NodeIndex) -> Result<im::HashSet<&devices::Host>> {
    let mut not_scsis = vec![];
    let mut scsis = im::hashset![];

    fn bin_device(
        nx: daggy::NodeIndex,
        not_scsis: &mut Vec<daggy::NodeIndex>,
        scsis: &mut im::HashSet<daggy::NodeIndex>,
        dag: &Dag,
    ) -> Result<()> {
        if self::is_scsi(self::get_device(dag, nx)?) {
            scsis.insert(nx);
        } else {
            not_scsis.push(nx)
        }

        Ok(())
    }

    bin_device(nx, &mut not_scsis, &mut scsis, &dag)?;

    while let Some(nx) = not_scsis.pop() {
        get_parents(dag, nx).try_for_each(|nx| bin_device(nx, &mut not_scsis, &mut scsis, &dag))?;
    }

    scsis
        .into_iter()
        .map(|n| -> Result<im::HashSet<&devices::Host>> {
            let results: im::HashSet<im::HashSet<&devices::Host>> =
                self::get_shared_and_self(dag, n)
                    .map(|n| self::get_hosts_from_scsi(dag, n))
                    .collect::<Result<im::HashSet<im::HashSet<&devices::Host>>>>()?;

            Ok(results.into_iter().flat_map(|x| x).collect())
        })
        .enumerate()
        .fold(Ok(im::hashset![]), |xs1, (i, xs2)| {
            if i == 0 {
                return xs2;
            }

            Ok(xs1?.intersection(xs2?))
        })
}

pub fn into_db_records(dag: &Dag) -> Result<im::OrdSet<(im::OrdSet<db::DeviceHost>, db::Device)>> {
    dag.node_references()
        .filter(|(nx, _)| self::get_all_node_children(dag, *nx).next().is_none())
        .filter(|(nx, _)| self::get_all_node_parents(dag, *nx).next().is_some())
        .map(|(nx, d)| {
            let hosts: im::HashSet<Host> = match d {
                devices::Device::Host(_) | devices::Device::VolumeGroup(_) => Err(
                    Error::graph_error("Tried to create a db record from an unmountable device"),
                ),
                devices::Device::ScsiDevice(_)
                | devices::Device::Mpath(_)
                | devices::Device::Partition(_) => {
                    let hosts = self::get_distinct_hosts(dag, nx)?
                        .into_iter()
                        .map(Host::Active)
                        .collect();

                    let r: Result<im::HashSet<Host>> = Ok(hosts);

                    r
                }
                devices::Device::LogicalVolume(_)
                | devices::Device::MdRaid(_)
                | devices::Device::Zpool(_)
                | devices::Device::Dataset(_) => {
                    let active_hosts = self::get_active_hosts(dag, nx)?;

                    let mut inactive_hosts = self::get_distinct_hosts(dag, nx)?;

                    inactive_hosts.retain(|x| !active_hosts.contains(x));

                    let all_hosts = active_hosts
                        .into_iter()
                        .map(Host::Active)
                        .chain(inactive_hosts.into_iter().map(Host::Inactive))
                        .collect();

                    Ok(all_hosts)
                }
            }?;

            let d = d.as_mountable_storage_device().ok_or_else(|| {
                Error::graph_error(format!(
                    "Could not convert {:?} to mountable storage device",
                    d
                ))
            })?;

            let dev = db::Device::new(d.size(), &d.name(), &d.serial(), &d.filesystem_type());

            let dev_hosts = hosts
                .into_iter()
                .map(|host| match host {
                    Host::Active(host) => db::DeviceHost::new(
                        &d.paths(),
                        &host,
                        &d.name(),
                        &d.serial(),
                        &d.mount_path(),
                        true,
                    ),
                    Host::Inactive(host) => db::DeviceHost::new(
                        &d.paths(),
                        &host,
                        &d.name(),
                        &d.serial(),
                        &d.mount_path(),
                        false,
                    ),
                })
                .collect();

            Ok((dev_hosts, dev))
        })
        .collect()
}

/// Find and add any shared edges between devices
pub fn add_shared_edges(dag: &mut Dag) -> Result<()> {
    let edges: Vec<_> = {
        let shared = dag.node_references().fold(
            vec![],
            |mut xs: Vec<HashSet<(daggy::NodeIndex, &Device)>>, (n, d)| {
                let slot = xs
                    .iter()
                    .position(|ys| ys.iter().any(|(_, d2)| d.as_parent() == d2.as_parent()));

                match slot {
                    Some(i) => {
                        xs[i].insert((n, d));
                    }
                    None => xs.push(hashset![(n, d)]),
                }

                xs
            },
        );

        shared
            .iter()
            .map(|xs| {
                xs.iter()
                    .enumerate()
                    .fold(hashset![], |mut hs, (i, (n1, _))| {
                        for (n2, _) in xs.iter().skip(i + 1) {
                            hs.insert((*n1, *n2));
                        }

                        hs
                    })
            })
            .flatten()
            .collect()
    };

    log::debug!("Found these nodes to build edges on: {:?}", edges);

    for (n1, n2) in edges {
        dag.update_edge(n1, n2, Edge::Shared)?;
    }

    Ok(())
}

/// Find and recursively add parent edges to nodes in the graph
pub fn populate_parents(dag: &mut Dag, ro_dag: &Dag, node_idx: daggy::NodeIndex) -> Result<()> {
    let dev = ro_dag
        .node_weight(node_idx)
        .ok_or_else(|| Error::graph_error("Could not find device in graph"))?;

    let devs = match dev {
        Device::Host(_) => filter_refs(ro_dag, |d| is_scsi(d)),
        Device::Mpath(m) => {
            let parent = m.as_parent();

            filter_refs(ro_dag, |d| {
                is_vg(d, &parent)
                    || is_partition(d, &parent)
                    || is_md(d, &parent)
                    || is_pool(d, &parent)
            })
        }
        Device::ScsiDevice(s) => {
            let parent = s.as_parent();

            filter_refs(ro_dag, |d| {
                is_partition(d, &parent)
                    || is_mpath(d, &parent)
                    || is_vg(d, &parent)
                    || is_md(d, &parent)
                    || is_pool(d, &parent)
            })
        }
        Device::Partition(p) => {
            let parent = p.as_parent();

            filter_refs(ro_dag, |d| {
                is_partition(d, &parent)
                    || is_vg(d, &parent)
                    || is_md(d, &parent)
                    || is_pool(d, &parent)
            })
        }
        Device::VolumeGroup(vg) => {
            let parent = vg.as_parent();

            filter_refs(ro_dag, |d| is_lv(d, &parent))
        }
        Device::LogicalVolume(lv) => {
            let parent = lv.as_parent();

            filter_refs(ro_dag, |d| is_partition(d, &parent) || is_pool(d, &parent))
        }
        Device::MdRaid(m) => {
            let parent = m.as_parent();

            filter_refs(ro_dag, |d| {
                is_vg(d, &parent)
                    || is_partition(d, &parent)
                    || is_md(d, &parent)
                    || is_pool(d, &parent)
            })
        }
        Device::Zpool(z) => filter_refs(ro_dag, |d| is_dataset(d, &z.serial)),
        Device::Dataset(_) => vec![],
    };

    for idx in devs {
        dag.update_edge(node_idx, idx, Edge::Parent)?;

        populate_parents(dag, ro_dag, idx)?;
    }

    Ok(())
}

#[cfg(test)]
mod tests {
    use device_types::devices::{Device, DeviceType, Partition, Serial};

    #[test]
    fn is_scsi() {
        let scsi = Device::ScsiDevice(Default::default());

        let result = super::is_scsi(&scsi);

        assert_eq!(result, true);
    }

    #[test]
    fn is_not_scsi() {
        let scsi = Device::VolumeGroup(Default::default());

        let result = super::is_scsi(&scsi);

        assert_eq!(result, false);
    }
    #[test]
    fn is_partition() {
        let mut partition: Partition = Default::default();

        let parent = (DeviceType::ScsiDevice, Serial("3".to_string()));

        partition.parents.insert(parent.clone());

        let result = super::is_partition(&Device::Partition(partition), &parent);

        assert_eq!(result, true);
    }

    #[test]
    fn is_not_partition() {
        let parent = (DeviceType::ScsiDevice, Serial("3".to_string()));

        let result = super::is_partition(&Device::ScsiDevice(Default::default()), &parent);

        assert_eq!(result, false);
    }
}
