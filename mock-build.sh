#!/bin/bash


cd /builddir/

rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm
yum install -y make dotnet-sdk-2.1 mono-devel nodejs npm
export DOTNET_CLI_TELEMETRY_OPTOUT=1
npm i --ignore-scripts
npm run restore
dotnet fable npm-build
npm pack
rpmlint /builddir/*.spec
make DRYRUN=false srpm

chown -R mockbuild:mock /builddir
su - mockbuild <<EOF
set -xe
cd /builddir/
mock _topdir/SRPMS/*.src.rpm --resultdir="/builddir" --enable-network
EOF
