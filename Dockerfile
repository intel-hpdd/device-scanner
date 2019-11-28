FROM rust:1.39 as builder
WORKDIR /build
COPY . .
RUN cargo build -p device-aggregator --release

FROM ubuntu
COPY --from=builder /build/target/release/device-aggregator /usr/local/bin

CMD ["device-aggregator"]