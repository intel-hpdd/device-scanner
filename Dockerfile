FROM imlteam/dotnet-docker as builder
WORKDIR /build
COPY . .
RUN npm install --only=dev
RUN npm run restore
RUN dotnet fable npm-build

FROM node:alpine
WORKDIR /root
COPY --from=builder /build/dist/device-aggregator-daemon/device-aggregator-daemon .
CMD ["node", "./device-aggregator-daemon"]
