FROM rust:1.39 as builder
WORKDIR /build
COPY . .
RUN cargo build -p device-aggregator --release

CMD ["device-aggregator"]