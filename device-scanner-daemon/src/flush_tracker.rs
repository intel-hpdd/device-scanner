use std::collections::HashMap;

#[derive(PartialEq, Debug)]
pub struct FlushTracker {
    flush_points: HashMap<usize, usize>,
    total: usize,
}

impl FlushTracker {
    pub fn new(xs: Vec<usize>, total: usize) -> Self {
        println!("new tracker with ids: {:?}", xs);

        FlushTracker {
            flush_points: xs.into_iter().map(|x| (x, 0)).collect(),
            total,
        }
    }

    pub fn advance(&mut self, id: usize, x: usize) {
        self.flush_points = self
            .flush_points
            .iter()
            .map(|(k, &v)| if &id == k { (*k, v + x) } else { (*k, v) })
            .filter(|(_, v)| *v < self.total)
            .collect();
    }

    pub fn slice_point(&self, id: usize) -> usize {
        *self
            .flush_points
            .get(&id)
            .unwrap_or_else(|| panic!("Could not get flush_point at id {}", id))
    }

    pub fn remove(&mut self, id: usize) {
        println!("removing id {}", id);

        self.flush_points.remove(&id);
    }

    pub fn has_more(&self) -> bool {
        self.flush_points.values().any(|&v| v < self.total)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_creation() {
        let xs = vec![1, 3, 4, 6];

        let ft = FlushTracker::new(xs, 10);

        let mut hm = HashMap::new();
        hm.insert(1, 0);
        hm.insert(3, 0);
        hm.insert(4, 0);
        hm.insert(6, 0);

        assert_eq!(
            ft,
            FlushTracker {
                flush_points: hm,
                total: 10
            }
        )
    }

    #[test]
    fn test_has_more() {
        let mut ft = FlushTracker::new(vec![1], 10);

        assert!(ft.has_more());

        ft.advance(1, 5);

        assert!(ft.has_more());

        ft.advance(1, 4);

        assert!(ft.has_more());

        ft.advance(1, 1);

        assert_eq!(ft.has_more(), false);

        assert_eq!(ft.flush_points.len(), 0)
    }

    #[test]
    fn test_remove() {
        let mut ft = FlushTracker::new(vec![1], 10);

        ft.remove(1);

        assert_eq!(ft.has_more(), false);

        assert_eq!(ft.flush_points.len(), 0)
    }
}
