%define     base_name device-scanner
%define     proxy_name scanner-proxy
%define     mount_name mount-emitter
%define     aggregator_name device-aggregator
%define     base_prefixed iml-%{base_name}
%define     proxy_prefixed iml-%{proxy_name}
%define     mount_prefixed iml-%{mount_name}
%define     aggregator_prefixed iml-%{aggregator_name}
Name:       %{base_prefixed}
Version:    2.1.0
Release:    2%{?dist}
Summary:    Maintains data of block and ZFS devices
License:    MIT
Group:      System Environment/Libraries
URL:        https://github.com/intel-hpdd/%{base_name}
# Forcing local source because rpkg in copr does not seem to have a way
# to build source in the same way a package manager would.
Source0:    %{base_prefixed}-%{version}.tgz

ExclusiveArch: %{nodejs_arches}

BuildRequires: nodejs-packaging
BuildRequires: nodejs
BuildRequires: npm
BuildRequires: mono-devel
BuildRequires: %{?scl_prefix}rh-dotnet20

%{?systemd_requires}
BuildRequires: systemd

Requires: nodejs
Requires: iml-node-libzfs
Requires: socat

%description
device-scanner-daemon builds an in-memory representation of
devices using udev and zed.

%package proxy
Summary:    Forwards device-scanner updates to device-aggregator
License:    MIT
Group:      System Environment/Libraries
Requires:   %{base_prefixed} = %{version}-%{release}
%description proxy
scanner-proxy-daemon forwards device-scanner updates received

%package aggregator
Summary:    Assembles global device view from multiple device scanner instances.
License:    MIT
Group:      System Environment/Libraries
%description aggregator
device-aggregator-daemon aggregates data received from device
scanner instances over HTTPS.

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
mkdir -p %{buildroot}%{_presetdir}
cp dist/%{base_name}-daemon/%{base_name}.socket %{buildroot}%{_unitdir}
cp dist/%{base_name}-daemon/%{base_name}.service %{buildroot}%{_unitdir}
cp dist/%{base_name}-daemon/99-%{base_name}.preset %{buildroot}%{_presetdir}
cp dist/%{proxy_name}-daemon/%{proxy_name}.service %{buildroot}%{_unitdir}
cp dist/%{proxy_name}-daemon/%{proxy_name}.path %{buildroot}%{_unitdir}
cp dist/%{proxy_name}-daemon/99-%{proxy_name}.preset %{buildroot}%{_presetdir}
cp dist/%{aggregator_name}-daemon/%{aggregator_name}.socket %{buildroot}%{_unitdir}
cp dist/%{aggregator_name}-daemon/%{aggregator_name}.service %{buildroot}%{_unitdir}
cp dist/%{aggregator_name}-daemon/99-%{aggregator_name}.preset %{buildroot}%{_presetdir}
cp dist/listeners/%{mount_name}.service %{buildroot}%{_unitdir}

mkdir -p %{buildroot}%{_libdir}/%{base_prefixed}-daemon
cp dist/%{base_name}-daemon/%{base_name}-daemon %{buildroot}%{_libdir}/%{base_prefixed}-daemon

mkdir -p %{buildroot}%{_libdir}/%{proxy_prefixed}-daemon
cp dist/%{proxy_name}-daemon/%{proxy_name}-daemon %{buildroot}%{_libdir}/%{proxy_prefixed}-daemon

mkdir -p %{buildroot}%{_libdir}/%{aggregator_prefixed}-daemon
cp dist/%{aggregator_name}-daemon/%{aggregator_name}-daemon %{buildroot}%{_libdir}/%{aggregator_prefixed}-daemon

mkdir -p %{buildroot}%{_libdir}/%{mount_prefixed}
cp dist/listeners/%{mount_name} %{buildroot}%{_libdir}/%{mount_prefixed}

mkdir -p %{buildroot}/lib/udev
cp dist/listeners/udev-listener %{buildroot}/lib/udev/udev-listener

mkdir -p %{buildroot}%{_sysconfdir}/udev/rules.d
cp dist/listeners/99-iml-%{base_name}.rules %{buildroot}%{_sysconfdir}/udev/rules.d

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
%dir %{_libdir}/%{base_prefixed}-daemon
%attr(0755,root,root)%{_libdir}/%{base_prefixed}-daemon/%{base_name}-daemon
%attr(0644,root,root)%{_unitdir}/%{base_name}.service
%attr(0644,root,root)%{_unitdir}/%{base_name}.socket
%attr(0644,root,root)%{_presetdir}/99-%{base_name}.preset
%dir %{_libdir}/%{mount_prefixed}
%attr(0755,root,root)%{_libdir}/%{mount_prefixed}/%{mount_name}
%attr(0644,root,root)%{_unitdir}/%{mount_name}.service
%attr(0755,root,root)/lib/udev/udev-listener
%attr(0644,root,root)%{_sysconfdir}/udev/rules.d/99-iml-%{base_name}.rules
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/history_event-scanner.sh
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/pool_create-scanner.sh
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/pool_destroy-scanner.sh
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/pool_export-scanner.sh
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/pool_import-scanner.sh
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/vdev_add-scanner.sh
%{_sysconfdir}/zfs/zed.d/*.sh

%files proxy
%dir %{_libdir}/%{proxy_prefixed}-daemon
%attr(0755,root,root)%{_libdir}/%{proxy_prefixed}-daemon/%{proxy_name}-daemon
%attr(0644,root,root)%{_unitdir}/%{proxy_name}.service
%attr(0644,root,root)%{_unitdir}/%{proxy_name}.path
%attr(0644,root,root)%{_presetdir}/99-%{proxy_name}.preset

%files aggregator
%dir %{_libdir}/%{aggregator_prefixed}-daemon
%attr(0755,root,root)%{_libdir}/%{aggregator_prefixed}-daemon/%{aggregator_name}-daemon
%attr(0644,root,root)%{_unitdir}/%{aggregator_name}.service
%attr(0644,root,root)%{_unitdir}/%{aggregator_name}.socket
%attr(0644,root,root)%{_presetdir}/99-%{aggregator_name}.preset

%triggerin -- zfs > 0.7.4
if modprobe zfs; then
  systemctl enable zfs-zed.service
  systemctl start zfs-zed.service
  systemctl kill -s SIGHUP zfs-zed.service
  echo '{"ZedCommand":"Init"}' | socat - UNIX-CONNECT:/var/run/%{base_name}.sock
fi

%post
%systemd_post %{base_name}.socket
%systemd_post %{base_name}.service
%systemd_post %{mount_name}.service

if [ $1 -eq 1 ]; then
  systemctl start %{base_name}.socket
  udevadm trigger --action=change --subsystem-match=block
fi

%post proxy
%systemd_post %{proxy_name}.path

if [ $1 -eq 1 ]; then
  systemctl start %{proxy_name}.path
fi

%post aggregator
%systemd_post %{aggregator_name}.socket

if [ $1 -eq 1 ]; then
  systemctl start %{aggregator_name}.socket
fi

%preun
%systemd_preun %{base_name}.socket
%systemd_preun %{base_name}.service

if [ $1 -eq 0 ]; then
  rm /var/run/%{base_name}.sock
fi

%preun proxy
%systemd_preun %{proxy_name}.path
%systemd_preun %{proxy_name}.service

%preun aggregator
%systemd_preun %{aggregator_name}.socket
%systemd_preun %{aggregator_name}.service

if [ $1 -eq 0 ] ; then
  rm /var/run/%{aggregator_name}.sock
fi

%postun
%systemd_postun_with_restart %{base_name}.socket

if [ $1 -ge 1 ] ; then
  udevadm trigger --action=change --subsystem-match=block
  systemctl start %{mount_name}.service
fi

%postun proxy
%systemd_postun_with_restart %{proxy_name}.path

%postun aggregator
%systemd_postun_with_restart %{aggregator_name}.socket

%changelog
* Mon Feb 26 2018 Tom Nabarro <tom.nabarro@intel.com> - 2.1.0-2
- Make scanner-proxy a sub-package (separate rpm)
- Handle upgrade scenarios

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
