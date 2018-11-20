%define     base_name device-scanner
%define     device_types device-types-0.1.0
%define     futures_failure futures-failure-0.1.0
%define	    iml_device_fns iml-device-fns-0.1.0

Name:       iml-%{base_name}
Version:    2.0.0
Release:    1%{?dist}
Summary:    Maintains data of block and ZFS devices
License:    MIT
Group:      System Environment/Libraries
URL:        https://github.com/whamcloud/%{base_name}
Source0:    device-scanner-daemon-%{version}.crate
Source1:    uevent-listener-0.1.0.crate
Source2:    mount-emitter-0.1.0.crate
Source3:    device-scanner-proxy-0.1.0.crate
Source4:    device-aggregator-2.0.0.crate
Source5:    device-scanner-zedlets-0.1.0.crate
Source6:    zed-enhancer-0.1.0.crate
Source7:    %{device_types}.crate
Source8:    %{futures_failure}.crate
Source9:    %{iml_device_fns}.create

%{?systemd_requires}
BuildRequires: systemd
BuildRequires: gcc
BuildRequires: openssl-devel
BuildRequires: clang-5.0.0
BuildRequires: libzfs2-devel
BuildRequires: zfs >= 0.7.9
BuildRequires: cargo

Requires: socat

%description
device-scanner-daemon builds an in-memory representation of
devices using udev, zed and findmnt.


%package proxy
Summary:    Forwards device-scanner updates to device-aggregator
License:    MIT
Group:      System Environment/Libraries
Requires:   %{name} = %{version}-%{release}
%description proxy
scanner-proxy-daemon forwards device-scanner updates received


%package aggregator
Summary:    Assembles global device view from multiple device scanner instances.
License:    MIT
Group:      System Environment/Libraries
Autoreq:    0
%description aggregator
device-aggregator aggregates data received from device
scanner instances.


%prep
%setup -T -D -b 9 -n %{iml_device_fns}

%setup -T -D -b 8 -n %{futures_failure}

%setup -T -D -b 7 -n %{device_types}

%setup -T -D -b 6 -n zed-enhancer-0.1.0

%setup -T -D -b 5 -n device-scanner-zedlets-0.1.0

%setup -T -D -b 4 -n device-aggregator-2.0.0

%setup -T -D -b 3 -n device-scanner-proxy-0.1.0

%setup -T -D -b 2 -n mount-emitter-0.1.0

%setup -T -D -b 1 -n uevent-listener-0.1.0

%setup -T -D -b 0 -n device-scanner-daemon-2.0.0


%build
cat << EOF > ../patch.txt
[patch.crates-io]
device-types = { path = "../%{device_types}" }
futures-failure = { path = "../%{futures_failure}" }
iml-device-fns = { path = "../%{iml_device_fns}" }
EOF

cat ../patch.txt >> Cargo.toml
cargo build --release

cd ../device-scanner-proxy-0.1.0
cat ../patch.txt >> Cargo.toml
cargo build --release

cd ../device-aggregator-2.0.0
cat ../patch.txt >> Cargo.toml
cargo build --release

cd ../uevent-listener-0.1.0
cat ../patch.txt >> Cargo.toml
cargo build --release

cd ../mount-emitter-0.1.0
cat ../patch.txt >> Cargo.toml
cargo build --release

cd ../device-scanner-zedlets-0.1.0
cat ../patch.txt >> Cargo.toml
cargo build --release

cd ../zed-enhancer-0.1.0
cat ../patch.txt >> Cargo.toml
cargo build --release

%clean
rm -rf %{buildroot}


%install
mkdir -p %{buildroot}%{_bindir}
mkdir -p %{buildroot}%{_unitdir}
mkdir -p %{buildroot}%{_presetdir}
mkdir -p %{buildroot}%{_sysconfdir}/udev/rules.d

cp systemd-units/device-scanner.{target,socket,service} %{buildroot}%{_unitdir}
cp systemd-units/block-device-populator.service %{buildroot}%{_unitdir}
cp systemd-units/00-device-scanner.preset %{buildroot}%{_presetdir}
cp target/release/device-scanner-daemon %{buildroot}%{_bindir}

cd ../device-scanner-proxy-0.1.0
cp systemd-units/scanner-proxy.{service,path} %{buildroot}%{_unitdir}
cp systemd-units/00-scanner-proxy.preset %{buildroot}%{_presetdir}
cp target/release/device-scanner-proxy %{buildroot}%{_bindir}

cd ../device-aggregator-2.0.0
cp systemd-units/device-aggregator.service %{buildroot}%{_unitdir}
cp systemd-units/00-device-aggregator.preset %{buildroot}%{_presetdir}
cp target/release/device-aggregator %{buildroot}%{_bindir}

cd ../uevent-listener-0.1.0
cp udev-rules/99-iml-device-scanner.rules %{buildroot}%{_sysconfdir}/udev/rules.d
cp target/release/uevent-listener %{buildroot}%{_bindir}

cd ../mount-emitter-0.1.0
cp systemd-units/mount-emitter.service %{buildroot}%{_unitdir}
cp systemd-units/mount-populator.service %{buildroot}%{_unitdir}
cp systemd-units/swap-emitter.service %{buildroot}%{_unitdir}
cp systemd-units/swap-emitter.timer %{buildroot}%{_unitdir}
cp target/release/mount-emitter %{buildroot}%{_bindir}

mkdir -p %{buildroot}%{_libexecdir}/zfs/zed.d
cd ../device-scanner-zedlets-0.1.0
cp target/release/pool_create-scanner %{buildroot}%{_libexecdir}/zfs/zed.d
cp target/release/pool_import-scanner %{buildroot}%{_libexecdir}/zfs/zed.d
cp target/release/vdev_add-scanner %{buildroot}%{_libexecdir}/zfs/zed.d
cp target/release/pool_destroy-scanner %{buildroot}%{_libexecdir}/zfs/zed.d
cp target/release/history_event-scanner %{buildroot}%{_libexecdir}/zfs/zed.d
cp target/release/pool_export-scanner %{buildroot}%{_libexecdir}/zfs/zed.d

mkdir -p %{buildroot}%{_sysconfdir}/zfs/zed.d
ln -sf %{_libexecdir}/zfs/zed.d/pool_create-scanner %{buildroot}%{_sysconfdir}/zfs/zed.d
ln -sf %{_libexecdir}/zfs/zed.d/pool_import-scanner %{buildroot}%{_sysconfdir}/zfs/zed.d
ln -sf %{_libexecdir}/zfs/zed.d/vdev_add-scanner %{buildroot}%{_sysconfdir}/zfs/zed.d
ln -sf %{_libexecdir}/zfs/zed.d/pool_destroy-scanner %{buildroot}%{_sysconfdir}/zfs/zed.d
ln -sf %{_libexecdir}/zfs/zed.d/history_event-scanner %{buildroot}%{_sysconfdir}/zfs/zed.d
ln -sf %{_libexecdir}/zfs/zed.d/pool_export-scanner %{buildroot}%{_sysconfdir}/zfs/zed.d


cd ../zed-enhancer-0.1.0
cp systemd-units/zed-enhancer.{service,socket} %{buildroot}%{_unitdir}
cp systemd-units/zed-populator.service %{buildroot}%{_unitdir}
cp systemd-units/00-zed-enhancer.preset %{buildroot}%{_presetdir}
cp target/release/zed-enhancer %{buildroot}%{_bindir}
cp udev-rules/99-iml-zed-enhancer.rules %{buildroot}%{_sysconfdir}/udev/rules.d

%files
%attr(0644,root,root)%{_unitdir}/block-device-populator.service
%attr(0644,root,root)%{_unitdir}/device-scanner.target
%attr(0644,root,root)%{_unitdir}/device-scanner.socket
%attr(0644,root,root)%{_unitdir}/device-scanner.service
%attr(0644,root,root)%{_unitdir}/mount-emitter.service
%attr(0644,root,root)%{_unitdir}/mount-populator.service
%attr(0644,root,root)%{_unitdir}/swap-emitter.timer
%attr(0644,root,root)%{_unitdir}/swap-emitter.service
%attr(0644,root,root)%{_unitdir}/zed-enhancer.service
%attr(0644,root,root)%{_unitdir}/zed-enhancer.socket
%attr(0644,root,root)%{_unitdir}/zed-populator.service
%attr(0644,root,root)%{_presetdir}/00-zed-enhancer.preset
%attr(0644,root,root)%{_presetdir}/00-device-scanner.preset
%attr(0644,root,root)%{_sysconfdir}/udev/rules.d/99-iml-device-scanner.rules
%attr(0644,root,root)%{_sysconfdir}/udev/rules.d/99-iml-zed-enhancer.rules
%attr(0755,root,root)%{_bindir}/device-scanner-daemon
%attr(0755,root,root)%{_bindir}/uevent-listener
%attr(0755,root,root)%{_bindir}/mount-emitter
%attr(0755,root,root)%{_bindir}/zed-enhancer
%attr(0755,root,root)%{_libexecdir}/zfs/zed.d/*
%{_sysconfdir}/zfs/zed.d/*


%files proxy
%attr(0644,root,root)%{_unitdir}/scanner-proxy.service
%attr(0644,root,root)%{_unitdir}/scanner-proxy.path
%attr(0644,root,root)%{_presetdir}/00-scanner-proxy.preset
%attr(0755,root,root)%{_bindir}/device-scanner-proxy

%files aggregator
%attr(0644,root,root)%{_unitdir}/device-aggregator.service
%attr(0644,root,root)%{_presetdir}/00-device-aggregator.preset
%attr(0755,root,root)%{_bindir}/device-aggregator


%post
systemctl preset device-scanner.socket
systemctl preset mount-emitter.service
systemctl preset swap-emitter.timer
systemctl preset zed-enhancer.socket


%post proxy
systemctl preset scanner-proxy.path

%post aggregator
systemctl preset device-aggregator.service

%preun
%systemd_preun device-scanner.target
%systemd_preun device-scanner.socket
%systemd_preun device-scanner.service
%systemd_preun mount-emitter.service
%systemd_preun block-device-populator.service
%systemd_preun zed-populator.service
%systemd_preun mount-populator.service
%systemd_preun swap-emitter.timer
%systemd_preun swap-emitter.service
%systemd_preun zed-enhancer.socket
%systemd_preun zed-enhancer.service


%preun proxy
%systemd_preun scanner-proxy.path
%systemd_preun scanner-proxy.service


%preun aggregator
%systemd_preun device-aggregator.service

%postun
%systemd_postun_with_restart device-scanner.socket
%systemd_postun_with_restart zed-enhancer.socket


%postun proxy
%systemd_postun_with_restart scanner-proxy.path

%postun aggregator
%systemd_postun_with_restart device-aggregator.service

%changelog
* Thu Oct 18 2018 Joe Grund <jgrund@whamcloud.com> 2.0.0-1
- Resolve device graph agent-side
- Rewrite in Rust

* Tue Jun 26 2018 Joe Grund <joe.grund@whamcloud.com> - 2.0.0-1
- Remove module-tools
- Remove vg_size check

* Mon May 14 2018 Tom Nabarro <tom.nabarro@intel.com> - 2.0.0-1
- Add mount detection to device-scanner
- Integrate device-aggregator
- Move device munging inside aggregator

* Mon Feb 26 2018 Tom Nabarro <tom.nabarro@intel.com> - 2.0.0-1
- Make scanner-proxy a sub-package (separate rpm)
- Handle upgrade scenarios

* Thu Feb 15 2018 Tom Nabarro <tom.nabarro@intel.com> - 2.0.0-1
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