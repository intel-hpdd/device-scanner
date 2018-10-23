extern crate failure;
extern crate futures;
extern crate futures_failure;
extern crate hyper;
extern crate native_tls;
extern crate tokio;
extern crate tokio_tls;

use std::{io, sync::Arc};
use tokio::net::TcpStream;

use tokio_tls::{TlsConnector, TlsStream};

use failure::{Error, ResultExt};
use futures::future::{err, Future};
use futures_failure::FutureExt;

use native_tls::Identity;

use hyper::{
    client::{
        connect::{Connect, Connected, Destination},
        HttpConnector,
    },
    header::HeaderValue,
    Body, Client, Method, Request,
};

use tokio::prelude::*;

/// Builds a `Uri` out of a given string
pub fn build_uri(url: &str) -> Result<hyper::Uri, Error> {
    let uri: hyper::Uri = url.parse()?;
    let mut parts = uri.into_parts();
    parts.path_and_query = Some("/iml-device-aggregator".parse()?);
    hyper::Uri::from_parts(parts).map_err(Error::from)
}

/// Sends a message to device-aggregator
pub fn send_message(
    uri: &hyper::Uri,
    json: String,
    pfx: &[u8],
) -> impl Future<Item = (), Error = Error> {
    let mut req = Request::new(Body::from(json));
    *req.method_mut() = Method::POST;
    *req.uri_mut() = uri.clone();
    req.headers_mut().insert(
        hyper::header::CONTENT_TYPE,
        HeaderValue::from_static("application/json"),
    );

    build_https_client(pfx)
        .into_future()
        .and_then(|c| c.request(req).context("making a request"))
        .map(|_| ())
}

/// Creates a https client that will send requests to device-aggregator
fn build_https_client(pfx: &[u8]) -> Result<Client<HttpsConnector>, Error> {
    let id = Identity::from_pkcs12(pfx, "").context("getting id from pfx")?;
    let tls_conn = TlsConnector::from(
        native_tls::TlsConnector::builder()
            .identity(id)
            .build()
            .context("building TlsConnector")?,
    );
    let mut https_conn = HttpsConnector::new(tls_conn);

    https_conn.http.enforce_http(false);

    Ok(Client::builder().build(https_conn))
}

pub struct HttpsConnector {
    tls: Arc<TlsConnector>,
    pub http: HttpConnector,
}

impl HttpsConnector {
    pub fn new(x: TlsConnector) -> Self {
        HttpsConnector {
            tls: Arc::new(x),
            http: HttpConnector::new(2),
        }
    }
}

impl Connect for HttpsConnector {
    type Transport = TlsStream<TcpStream>;
    type Error = io::Error;
    type Future = Box<Future<Item = (Self::Transport, Connected), Error = Self::Error> + Send>;

    fn connect(&self, dst: Destination) -> Self::Future {
        if dst.scheme() != "https" {
            return Box::new(err(io::Error::new(
                io::ErrorKind::Other,
                "only works with https",
            )));
        }

        let host = format!(
            "{}{}",
            dst.host(),
            dst.port()
                .map(|p| format!(":{}", p))
                .unwrap_or_else(|| "".into())
        );

        let tls_cx = self.tls.clone();
        Box::new(self.http.connect(dst).and_then(move |(tcp, connected)| {
            tls_cx
                .connect(&host, tcp)
                .map(|s| (s, connected))
                .map_err(|e| io::Error::new(io::ErrorKind::Other, e))
        }))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn building_a_uri() -> Result<(), Error> {
        let out = build_uri("http://adm.local:8080")?;
        let expected: hyper::Uri = "http://adm.local:8080/iml-device-aggregator".parse()?;

        assert_eq!(out, expected);

        Ok(())
    }
}
