%define     base_name device-scanner
%define     prefix_name iml-%{base_name}
%define     proxy_base_name scanner-proxy
%define     proxy_prefix_name iml-%{proxy_base_name}
Name:       %{prefix_name}
Version:    2.1.0
Release:    2%{?dist}
Summary:    Maintains data of block and ZFS devices
License:    MIT
Group:      System Environment/Libraries
URL:        https://github.com/intel-hpdd/%{base_name}
# Forcing local source because rpkg in copr does not seem to have a way
# to build source in the same way a package manager would.
Source0:    %{prefix_name}-%{version}.tgz

ExclusiveArch: %{nodejs_arches}

BuildRequires: nodejs-packaging
BuildRequires: nodejs
BuildRequires: npm
BuildRequires: mono-devel
BuildRequires: %{?scl_prefix}rh-dotnet20
BuildRequires: systemd

Requires: nodejs
Requires: iml-node-libzfs
Requires: socat

%description
device-scanner-daemon builds an in-memory representation of
devices using udev and zed.

%package iml-scanner-proxy
Summary:    Forwards device-scanner updates to device-aggregator
License:    MIT
Group:      System Environment/Libraries
%description iml-scanner-proxy
scanner-proxy-daemon forwards device-scanner updates received
on local socket to the device aggregator over HTTPS.

%prep
%setup

%build
mozroots --import --sync
scl enable rh-dotnet20 - << EOF
set -e
export DOTNET_CLI_TELEMETRY_OPTOUT=1
npm i --ignore-scripts
npm run restore
dotnet fable npm-build
EOF

%install
mkdir -p %{buildroot}%{_unitdir}
cp dist/device-scanner-daemon/%{base_name}.socket %{buildroot}%{_unitdir}
cp dist/device-scanner-daemon/%{base_name}.service %{buildroot}%{_unitdir}
cp dist/scanner-proxy-daemon/%{proxy_base_name}.service %{buildroot}%{_unitdir}
cp dist/scanner-proxy-daemon/%{proxy_base_name}.path %{buildroot}%{_unitdir}

mkdir -p %{buildroot}%{_libdir}/%{prefix_name}-daemon
cp dist/device-scanner-daemon/device-scanner-daemon %{buildroot}%{_libdir}/%{prefix_name}-daemon

mkdir -p %{buildroot}%{_libdir}/%{proxy_prefix_name}-daemon
cp dist/scanner-proxy-daemon/scanner-proxy-daemon %{buildroot}%{_libdir}/%{proxy_prefix_name}-daemon

mkdir -p %{buildroot}/lib/udev
cp dist/listeners/udev-listener %{buildroot}/lib/udev/udev-listener

mkdir -p %{buildroot}%{_sysconfdir}/udev/rules.d
cp dist/listeners/99-iml-device-scanner.rules %{buildroot}%{_sysconfdir}/udev/rules.d

mkdir -p %{buildroot}%{_libexecdir}/zfs/zed.d/
cp dist/listeners/history_event-scanner.sh %{buildroot}%{_libexecdir}/zfs/zed.d/history_event-scanner.sh
cp dist/listeners/pool_create-scanner.sh %{buildroot}%{_libexecdir}/zfs/zed.d/pool_create-scanner.sh
cp dist/listeners/pool_destroy-scanner.sh %{buildroot}%{_libexecdir}/zfs/zed.d/pool_destroy-scanner.sh
cp dist/listeners/pool_export-scanner.sh %{buildroot}%{_libexecdir}/zfs/zed.d/pool_export-scanner.sh
cp dist/listeners/pool_import-scanner.sh %{buildroot}%{_libexecdir}/zfs/zed.d/pool_import-scanner.sh
cp dist/listeners/vdev_add-scanner.sh %{buildroot}%{_libexecdir}/zfs/zed.d/vdev_add-scanner.sh


mkdir -p %{buildroot}%{_sysconfdir}/zfs/zed.d/
ln -sf %{_libexecdir}/zfs/zed.d/history_event-scanner.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/history_event-scanner.sh
ln -sf %{_libexecdir}/zfs/zed.d/pool_create-scanner.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/pool_create-scanner.sh
ln -sf %{_libexecdir}/zfs/zed.d/pool_destroy-scanner.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/pool_destroy-scanner.sh
ln -sf %{_libexecdir}/zfs/zed.d/pool_export-scanner.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/pool_export-scanner.sh
ln -sf %{_libexecdir}/zfs/zed.d/pool_import-scanner.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/pool_import-scanner.sh
ln -sf %{_libexecdir}/zfs/zed.d/vdev_add-scanner.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/vdev_add-scanner.sh

%clean
rm -rf %{buildroot}

%files
%dir %{_libdir}/%{prefix_name}-daemon
%attr(0755,root,root)%{_libdir}/%{prefix_name}-daemon/device-scanner-daemon
%attr(0644,root,root)%{_unitdir}/%{base_name}.service
%attr(0644,root,root)%{_unitdir}/%{base_name}.socket
%attr(0755,root,root)/lib/udev/udev-listener
%attr(0644,root,root)%{_sysconfdir}/udev/rules.d/99-iml-device-scanner.rules
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/history_event-scanner.sh
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/pool_create-scanner.sh
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/pool_destroy-scanner.sh
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/pool_export-scanner.sh
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/pool_import-scanner.sh
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/vdev_add-scanner.sh
%{_sysconfdir}/zfs/zed.d/*.sh

%files iml-scanner-proxy
%dir %{_libdir}/%{proxy_prefix_name}-daemon
%attr(0755,root,root)%{_libdir}/%{proxy_prefix_name}-daemon/scanner-proxy-daemon
%attr(0644,root,root)%{_unitdir}/%{proxy_base_name}.service
%attr(0644,root,root)%{_unitdir}/%{proxy_base_name}.path

%triggerin -- zfs >= 0.7.5

/sbin/modprobe zfs
systemctl enable zfs-zed.service
systemctl start zfs-zed.service
echo '{"ZedCommand":"Init"}' | socat - UNIX-CONNECT:/var/run/device-scanner.sock

%post
if [ $1 -eq 1 ] ; then
  systemctl enable %{base_name}.socket
  systemctl start %{base_name}.socket
  udevadm trigger --action=change --subsystem-match=block
fi

%post iml-scanner-proxy
if [ $1 -eq 1 ] ; then
  systemctl enable %{proxy_base_name}.path
  systemctl start %{proxy_base_name}.path
fi

%preun
if [ $1 -eq 0 ] ; then
  systemctl stop %{base_name}.socket
  systemctl disable %{base_name}.socket
  systemctl stop %{base_name}.service
  systemctl disable %{base_name}.service
  rm /var/run/%{base_name}.sock
fi

%preun iml-scanner-proxy
if [ $1 -eq 0 ] ; then
  systemctl stop %{proxy_base_name}.path
  systemctl disable %{proxy_base_name}.path
  systemctl stop %{proxy_base_name}.service
  systemctl disable %{proxy_base_name}.service
fi

%changelog
* Mon Feb 26 2018 Tom Nabarro <tom.nabarro@intel.com> - 2.1.0-2
- make scanner-proxy a sub-package (separate rpm)

* Thu Feb 15 2018 Tom Nabarro <tom.nabarro@intel.com> - 2.1.0-1
- Minor change, integrate scanner-proxy project

* Mon Jan 22 2018 Joe Grund <joe.grund@intel.com> - 2.0.0-1
- Breaking change, the API has changed output format


* Wed Sep 27 2017 Joe Grund <joe.grund@intel.com> - 1.1.1-1
- Fix bug where devices weren't removed.
- Cast empty IML_SIZE string to None.

* Thu Sep 21 2017 Joe Grund <joe.grund@intel.com> - 1.1.0-1
- Exclude unneeded devices.
- Get device ro status.
- Remove manual udev parsing.
- Remove socat as dep, device-scanner will listen to change events directly.

* Mon Sep 18 2017 Joe Grund <joe.grund@intel.com> - 1.0.2-1
- Fix missing keys to be option types.
- Add rules for scsi ids
- Add keys on change|add so we can `udevadm trigger` after install
- Trigger udevadm change event after install
- Read new state into scanner after install

* Tue Aug 29 2017 Joe Grund <joe.grund@intel.com> - 1.0.1-1
- initial package
