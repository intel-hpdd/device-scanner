use std::env;

/// Get the environment variable or panic
pub fn get_var(name: &str) -> String {
    env::var(name).unwrap_or_else(|_| format!("{} environment variable is required.", name))
}
