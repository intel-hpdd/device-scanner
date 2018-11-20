use device_types::devices::{self, AsParent, Device, Parent};
use std::path::PathBuf;

use im::{hashset, HashSet};

use daggy::{
    petgraph::{graph::Graph, visit::IntoNodeReferences},
    Dag,
};

#[derive(Copy, Clone, Debug)]
pub struct Weight;

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

fn get_vgs<'a>(
    graph: &'a Graph<Device, Weight>,
    parent: &'a Parent,
) -> impl Iterator<Item = (daggy::NodeIndex, &'a Device)> {
    graph.node_references().filter(move |(_, x)| match x {
        Device::VolumeGroup(vg) => vg.parents.contains(parent),
        _ => false,
    })
}

fn get_partitions<'a>(
    graph: &'a Graph<Device, Weight>,
    parent: &'a Parent,
) -> impl Iterator<Item = (daggy::NodeIndex, &'a Device)> {
    graph.node_references().filter(move |(_, x)| match x {
        Device::Partition(p) => p.parents.contains(parent),
        _ => false,
    })
}

fn get_lvs<'a>(
    graph: &'a Graph<Device, Weight>,
    parent: &'a Parent,
) -> impl Iterator<Item = (daggy::NodeIndex, &'a Device)> {
    graph.node_references().filter(move |(_, x)| match x {
        Device::LogicalVolume(lv) => parent == &lv.parent,
        _ => false,
    })
}

fn get_scsis<'a>(
    graph: &'a Graph<Device, Weight>,
) -> impl Iterator<Item = (daggy::NodeIndex, &'a Device)> {
    graph.node_references().filter(|(_, x)| match x {
        Device::ScsiDevice(_) => true,
        _ => false,
    })
}

fn get_mpaths<'a>(
    graph: &'a Graph<Device, Weight>,
    parent: &'a Parent,
) -> impl Iterator<Item = (daggy::NodeIndex, &'a Device)> {
    graph.node_references().filter(move |(_, x)| match x {
        Device::Mpath(m) => m.parents.contains(parent),
        _ => false,
    })
}

fn get_mds<'a>(
    graph: &'a Graph<Device, Weight>,
    parent: &'a Parent,
) -> impl Iterator<Item = (daggy::NodeIndex, &'a Device)> {
    graph.node_references().filter(move |(_, x)| match x {
        Device::MdRaid(m) => m.parents.contains(parent),
        _ => false,
    })
}

fn get_pools<'a>(
    graph: &'a Graph<Device, Weight>,
    paths: &'a HashSet<PathBuf>,
) -> impl Iterator<Item = (daggy::NodeIndex, &'a Device)> {
    graph.node_references().filter(move |(_, x)| match x {
        Device::Zpool(z) => {
            let vdev_paths = get_vdev_paths(&z.vdev);

            !paths.clone().intersection(vdev_paths).is_empty()
        }
        _ => false,
    })
}

fn get_datasets<'a>(
    graph: &'a Graph<Device, Weight>,
    guid: u64,
) -> impl Iterator<Item = (daggy::NodeIndex, &'a Device)> {
    graph.node_references().filter(move |(_, x)| match x {
        Device::Dataset(d) => d.pool_guid == guid,
        _ => false,
    })
}

pub fn build_dag(
    dag: &mut Dag<Device, Weight, u32>,
    graph: &Graph<Device, Weight>,
    node: &Device,
    node_idx: daggy::NodeIndex,
) -> Result<(), daggy::WouldCycle<Weight>> {
    match node {
        Device::Host(_) => {
            for (idx, n) in get_scsis(graph) {
                build_dag(dag, graph, n, idx)?;

                dag.update_edge(node_idx, idx, Weight)?;
            }
        }
        Device::Mpath(m) => {
            let parent = m.as_parent();

            let devs = get_vgs(graph, &parent)
                .chain(get_partitions(graph, &parent))
                .chain(get_mds(graph, &parent))
                .chain(get_pools(graph, &m.paths));

            for (idx, n) in devs {
                dag.update_edge(node_idx, idx, Weight)?;

                build_dag(dag, graph, n, idx)?;
            }
        }
        Device::ScsiDevice(s) => {
            let parent = s.as_parent();

            let devs = get_partitions(graph, &parent)
                // This should only be present for scsi devs
                .chain(get_mpaths(graph, &parent))
                .chain(get_vgs(graph, &parent))
                .chain(get_mds(graph, &parent))
                .chain(get_pools(graph, &s.paths));

            for (idx, n) in devs {
                dag.update_edge(node_idx, idx, Weight)?;

                build_dag(dag, graph, n, idx)?;
            }
        }
        Device::Partition(p) => {
            let parent = p.as_parent();

            let devs = get_partitions(graph, &parent)
                // This should only be present for scsi devs
                .chain(get_mpaths(graph, &parent))
                .chain(get_vgs(graph, &parent))
                .chain(get_mds(graph, &parent))
                .chain(get_pools(graph, &p.paths));

            for (idx, n) in devs {
                dag.update_edge(node_idx, idx, Weight)?;

                build_dag(dag, graph, n, idx)?;
            }
        }
        Device::VolumeGroup(vg) => {
            let parent = vg.as_parent();
            for (idx, n) in get_lvs(graph, &parent) {
                dag.update_edge(node_idx, idx, Weight)?;

                build_dag(dag, graph, n, idx)?;
            }
        }
        Device::LogicalVolume(lv) => {
            let parent = lv.as_parent();

            let devs = get_partitions(graph, &parent).chain(get_pools(graph, &lv.paths));

            for (idx, n) in devs {
                dag.update_edge(node_idx, idx, Weight)?;

                build_dag(dag, graph, n, idx)?;
            }
        }
        Device::MdRaid(m) => {
            let parent = m.as_parent();

            let devs = get_vgs(graph, &parent)
                .chain(get_partitions(graph, &parent))
                .chain(get_mds(graph, &parent))
                .chain(get_pools(graph, &m.paths));

            for (idx, n) in devs {
                dag.update_edge(node_idx, idx, Weight)?;

                build_dag(dag, graph, n, idx)?;
            }
        }
        Device::Zpool(devices::Zpool { guid, .. }) => {
            for (idx, n) in get_datasets(graph, *guid) {
                dag.update_edge(node_idx, idx, Weight)?;

                build_dag(dag, graph, n, idx)?;
            }
        }
        Device::Dataset(_) => {}
    };

    Ok(())
}
