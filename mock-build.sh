#!/bin/bash -xe

sed -i "s/\(config_opts\['chroot_setup_cmd']\) = '\(.*\)'/\1 = '\2 scl-utils-build'/" /etc/mock/default.cfg

ed <<"EOF" /etc/mock/default.cfg
$i

[copr-be.cloud.fedoraproject.org_results_managerforlustre_manager-for-lustre_epel-7-x86_64_]
name=added from: https://copr-be.cloud.fedoraproject.org/results/managerforlustre/manager-for-lustre-devel/epel-7-x86_64/
baseurl=https://copr-be.cloud.fedoraproject.org/results/managerforlustre/manager-for-lustre-devel/epel-7-x86_64/
enabled=1

[dotnet]
name=CentOS-7 - DotNet
baseurl=http://mirror.centos.org/centos/$releasever/dotnet/$basearch/
enabled=1

[mono-centos7-stable]
name=mono-centos7-stable
baseurl=http://download.mono-project.com/repo/centos7-stable/
enabled=1

.
w
q
EOF

chown -R mockbuild:mock /builddir

cd /builddir/
git clean -dfx
rpkg make-source
RELEASE=$(git rev-list HEAD | wc -l)

su - mockbuild <<EOF
set -xe
cd /builddir/
rpmlint \$PWD *.spec
rpmbuild -bs --define epel\ 1 --define package_release\ $RELEASE --define _srcrpmdir\ \$PWD --define _sourcedir\ \$PWD *.spec
mock iml-device-scanner-*.src.rpm -v --resultdir="/builddir" --rpmbuild-opts="--define package_release\ $RELEASE" --enable-network
EOF
