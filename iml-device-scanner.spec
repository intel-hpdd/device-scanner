%{?!package_release: %define package_release 1}

%define     base_name device-scanner
%define     prefix_name iml-%{base_name}
Name:       %{prefix_name}2
Version:    2.0.0
Release:    %{package_release}%{?dist}
Summary:    Builds an in-memory representation of devices. Uses udev rules to handle change events.
License:    MIT
Group:      System Environment/Libraries
URL:        https://github.com/intel-hpdd/%{base_name}
Source0:    http://registry.npmjs.org/@iml/%{base_name}/-/%{base_name}-%{version}.tgz

ExclusiveArch: %{nodejs_arches}

BuildRequires: nodejs-packaging
BuildRequires: systemd

Requires: nodejs
Requires: iml-node-libzfs
Requires: socat

Obsoletes: %{prefix_name}

%description
Builds an in-memory representation of devices using udev and zed.

%prep
%setup -q -n package

%build
#nothing to do

%install
mkdir -p %{buildroot}%{_unitdir}
cp dist/device-scanner-daemon/%{base_name}.socket %{buildroot}%{_unitdir}
cp dist/device-scanner-daemon/%{base_name}.service %{buildroot}%{_unitdir}

mkdir -p %{buildroot}%{_libdir}/%{prefix_name}-daemon
cp dist/device-scanner-daemon/device-scanner-daemon %{buildroot}%{_libdir}/%{prefix_name}-daemon

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

%triggerin -- zfs
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

%preun
if [ $1 -eq 0 ] ; then
  systemctl stop %{base_name}.service
  systemctl disable %{base_name}.service
  systemctl stop %{base_name}.socket
  systemctl disable %{base_name}.socket
  rm /var/run/%{base_name}.sock
fi

%changelog
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