#!/bin/bash -xe


cd /builddir/

rpmlint /builddir/device-scanner.spec
yum install -y dnf
make -f /builddir/.copr/Makefile srpm spec="/builddir/device-scanner.spec" outdir="/builddir/"

chown -R mockbuild:mock /builddir
su - mockbuild <<EOF
set -xe
cd /builddir/
mock iml-device-scanner-*.src.rpm --resultdir="/builddir" --enable-network
EOF
