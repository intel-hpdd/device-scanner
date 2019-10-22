FROM rust:1.39 as builder
WORKDIR /build
COPY . .
RUN cargo build -p device-aggregator --release

FROM rust:1.39
COPY --from=builder /build/target/release/device-aggregator /usr/local/bin
RUN apt-get update \
  && apt-get install -y postgresql-client \
  && apt-get clean \
  && rm -rf /var/lib/apt/lists/*

COPY wait-for-dependencies.sh /usr/local/bin/
ENTRYPOINT [ "wait-for-dependencies.sh" ]
CMD ["device-aggregator"]