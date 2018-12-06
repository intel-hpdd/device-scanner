// Copyright (c) 2018 DDN. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

use aggregator_error;
use device_types::devices::{self, AsParent, Device, Parent, Type};
use std::collections::VecDeque;
use std::path::PathBuf;

use im::{hashset, HashSet};

use daggy::petgraph::{
    self, graph,
    visit::{Dfs, EdgeRef, IntoNodeReferences, Walker},
};

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

/// Traverses a VDev tree and returns back it's paths
pub fn get_vdev_paths(vdev: &libzfs_types::VDev) -> HashSet<PathBuf> {
    match vdev {
        libzfs_types::VDev::Disk { path, .. } => hashset![path.clone()],
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
fn is_pool(d: &Device, paths: &HashSet<PathBuf>) -> bool {
    match d {
        Device::Zpool(z) => {
            let vdev_paths = get_vdev_paths(&z.vdev);

            !paths.clone().intersection(vdev_paths).is_empty()
        }
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
pub fn add(l: &mut Dag, r: &Dag, root: daggy::NodeIndex) -> aggregator_error::Result<()> {
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

/// Given a dag and a starting node, walk up the parents
/// until the host is hit.
pub fn get_active_host(dag: &Dag, nx: daggy::NodeIndex) -> Option<devices::Host> {
    let graph = dag.graph();

    let mut nx = Some(nx);

    loop {
        match nx {
            Some(n) => {
                if let Some(Device::Host(host)) = graph.node_weight(n) {
                    break Some(host.clone());
                }

                nx = get_parents(dag, n).nth(0);
            }
            None => {
                break None;
            }
        }
    }
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

pub fn get_distinct_hosts2(dag: &Dag, nx: daggy::NodeIndex) -> im::HashSet<devices::Host> {
    // The current level of the graph we're
    // walking up
    let level = hashset![];

    hashset![]
}

/// Given a dag and a starting leaf device, walk up towards the root
/// collecting the set of hosts that can use this device.
pub fn get_distinct_hosts(dag: &Dag, nx: daggy::NodeIndex) -> im::HashSet<devices::Host> {
    let mut visited = im::hashset![];

    let mut all_hosts: im::HashSet<im::HashSet<devices::Host>> = im::hashset![];

    let mut queue = VecDeque::new();

    queue.push_back(nx);

    while let Some(n) = queue.pop_front() {
        if visited.contains(&n) {
            continue;
        }

        let shared_hosts: im::HashSet<devices::Host> = get_shared(dag, n)
            .filter_map(|x| get_active_host(dag, x))
            .collect();

        let parents = get_parents(dag, n).filter(|&x| {
            dag.node_weight(x)
                .map(|x| x.name() == devices::DeviceType::Host)
                .is_none()
        });

        all_hosts.insert(shared_hosts);

        queue.extend(parents);

        visited.insert(n);
    }

    all_hosts
        .into_iter()
        .enumerate()
        .fold(im::hashset![], |h, (idx, next)| {
            if idx == 0 {
                return next;
            }

            h.intersection(next)
        })
}

pub fn into_device_set(
    dag: &Dag,
) -> im::HashSet<(im::HashSet<devices::Host>, &devices::Device, devices::Host)> {
    dag.node_references()
        .filter(|(nx, _)| dag.children(*nx).iter(dag).count() == 0)
        .filter_map(|(nx, d)| get_active_host(dag, nx).map(|h| (nx, d, h)))
        .map(|(nx, d, h)| (get_distinct_hosts(dag, nx), d, h))
        .collect()
}

/// Find and add any shared edges between devices
pub fn add_shared_edges(dag: &mut Dag) -> aggregator_error::Result<()> {
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
            }).flatten()
            .collect()
    };

    log::debug!("Found these nodes to build edges on: {:?}", edges);

    for (n1, n2) in edges {
        dag.update_edge(n1, n2, Edge::Shared)?;
    }

    Ok(())
}

/// Find and recursively add parent edges to nodes in the graph
pub fn populate_parents(
    dag: &mut Dag,
    ro_dag: &Dag,
    node_idx: daggy::NodeIndex,
) -> Result<(), daggy::WouldCycle<Edge>> {
    let devs = match ro_dag.node_weight(node_idx).unwrap() {
        Device::Host(_) => filter_refs(ro_dag, |d| is_scsi(d)),
        Device::Mpath(m) => {
            let parent = m.as_parent();

            filter_refs(ro_dag, |d| {
                is_vg(d, &parent)
                    || is_partition(d, &parent)
                    || is_md(d, &parent)
                    || is_pool(d, &m.paths)
            })
        }
        Device::ScsiDevice(s) => {
            let parent = s.as_parent();

            filter_refs(ro_dag, |d| {
                is_partition(d, &parent)
                    || is_mpath(d, &parent)
                    || is_vg(d, &parent)
                    || is_md(d, &parent)
                    || is_pool(d, &s.paths)
            })
        }
        Device::Partition(p) => {
            let parent = p.as_parent();

            filter_refs(ro_dag, |d| {
                is_partition(d, &parent)
                    || is_vg(d, &parent)
                    || is_md(d, &parent)
                    || is_pool(d, &p.paths)
            })
        }
        Device::VolumeGroup(vg) => {
            let parent = vg.as_parent();

            filter_refs(ro_dag, |d| is_lv(d, &parent))
        }
        Device::LogicalVolume(lv) => {
            let parent = lv.as_parent();

            filter_refs(ro_dag, |d| {
                is_partition(d, &parent) || is_pool(d, &lv.paths)
            })
        }
        Device::MdRaid(m) => {
            let parent = m.as_parent();

            filter_refs(ro_dag, |d| {
                is_vg(d, &parent)
                    || is_partition(d, &parent)
                    || is_md(d, &parent)
                    || is_pool(d, &m.paths)
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
