%{?!package_release: %define package_release 1}

%define base_name device-scanner
Name:       iml-%{base_name}2
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

mkdir -p %{buildroot}%{_libdir}/%{name}-daemon
cp dist/device-scanner-daemon/device-scanner-daemon %{buildroot}%{_libdir}/%{name}-daemon

mkdir -p %{buildroot}/lib/udev
cp dist/event-listener/event-listener %{buildroot}/lib/udev

mkdir -p %{buildroot}%{_sysconfdir}/udev/rules.d
cp dist/event-listener/99-iml-device-scanner.rules %{buildroot}%{_sysconfdir}/udev/rules.d

mkdir -p %{buildroot}%{_libexecdir}/zfs/zed.d/
cp dist/event-listener/event-listener %{buildroot}%{_libexecdir}/zfs/zed.d/generic-listener.sh

mkdir -p %{buildroot}%{_sysconfdir}/zfs/zed.d/
ln -sf %{_libexecdir}/zfs/zed.d/generic-listener.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/pool_create-scanner.sh
ln -sf %{_libexecdir}/zfs/zed.d/generic-listener.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/pool_destroy-scanner.sh
ln -sf %{_libexecdir}/zfs/zed.d/generic-listener.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/pool_import-scanner.sh
ln -sf %{_libexecdir}/zfs/zed.d/generic-listener.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/pool_export-scanner.sh
ln -sf %{_libexecdir}/zfs/zed.d/generic-listener.sh %{buildroot}%{_sysconfdir}/zfs/zed.d/history_event-scanner.sh

%clean
rm -rf %{buildroot}

%files
%dir %{_libdir}/%{name}-daemon
%attr(0755,root,root)%{_libdir}/%{name}-daemon/device-scanner-daemon
%attr(0644,root,root)%{_unitdir}/%{base_name}.service
%attr(0644,root,root)%{_unitdir}/%{base_name}.socket
%attr(0755,root,root)/lib/udev/event-listener
%attr(0644,root,root)%{_sysconfdir}/udev/rules.d/99-iml-device-scanner.rules
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/generic-listener.sh
%{_sysconfdir}/zfs/zed.d/*.sh

%triggerin -- libzfs2
systemctl enable zfs-zed.service
systemctl start zfs-zed.service
echo '{ "ACTION": "trigger zed" }' | socat - UNIX-CONNECT:/var/run/device-scanner.sock

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