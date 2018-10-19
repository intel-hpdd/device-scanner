%define     base_name device-scanner
%define     proxy_name scanner-proxy
%define     mount_name mount-emitter
%define     aggregator_name device-aggregator
%define     proxy_prefixed iml-%{proxy_name}
%define     mount_prefixed iml-%{mount_name}
%define     aggregator_prefixed iml-%{aggregator_name}

Name:       iml-%{base_name}
Version:    2.0.0
Release:    1%{?dist}
Summary:    Maintains data of block and ZFS devices
License:    MIT
Group:      System Environment/Libraries
URL:        https://github.com/whamcloud/%{base_name}
Patch0:     device-scanner-daemon-dev.patch
Patch1:     uevent-listener-dev.patch
Patch2:     mount-emitter-dev.patch
Source0:    device-scanner-daemon-%{version}.crate
Source1:    uevent-listener-0.1.0.crate
Source2:    mount-emitter-0.1.0.crate
Source3:    device-types-0.1.0.crate


%{?systemd_requires}
BuildRequires: systemd
BuildRequires: cargo
BuildRequires: upx
BuildRequires: openssl-devel

Requires: socat


%description
device-scanner-daemon builds an in-memory representation of
devices using udev, zed and findmnt.


%prep
%setup -T -D -b 3 -n device-types-0.1.0

%setup -T -D -b 2 -n mount-emitter-0.1.0
%if "%{?devel_build}"
%patch2 -p0 -b .workspace_fixup
%endif

%setup -T -D -b 1 -n uevent-listener-0.1.0
%if "%{?devel_build}"
%patch1 -p0 -b .workspace_fixup
%endif

%setup -T -D -b 0 -n device-scanner-daemon-2.0.0
%if "%{?devel_build}"
%patch0 -p0 -b .workspace_fixup
%endif


%build
cargo build --release
upx target/release/device-scanner-daemon

cd ../uevent-listener-0.1.0
cargo build --release
upx target/release/uevent-listener

cd ../mount-emitter-0.1.0
cargo build --release
upx target/release/mount-emitter


%clean
rm -rf %{buildroot}


%install
mkdir -p %{buildroot}%{_bindir}
mkdir -p %{buildroot}%{_unitdir}
mkdir -p %{buildroot}%{_presetdir}
mkdir -p %{buildroot}%{_sysconfdir}/udev/rules.d

cp systemd-units/device-scanner.{target,socket,service} %{buildroot}%{_unitdir}
cp systemd-units/block-device-populator.service %{buildroot}%{_unitdir}
cp systemd-units/zed-populator.service %{buildroot}%{_unitdir}
cp systemd-units/00-device-scanner.preset %{buildroot}%{_presetdir}
cp target/release/device-scanner-daemon %{buildroot}%{_bindir}

cd ../uevent-listener-0.1.0
cp udev-rules/99-iml-device-scanner.rules %{buildroot}%{_sysconfdir}/udev/rules.d
cp target/release/uevent-listener %{buildroot}%{_bindir}

cd ../mount-emitter-0.1.0
cp systemd-units/mount-emitter.service %{buildroot}%{_unitdir}
cp systemd-units/mount-populator.service %{buildroot}%{_unitdir}
cp systemd-units/swap-emitter.service %{buildroot}%{_unitdir}
cp systemd-units/swap-emitter.timer %{buildroot}%{_unitdir}
cp target/release/mount-emitter %{buildroot}%{_bindir}


%files
%attr(0644,root,root)%{_unitdir}/block-device-populator.service
%attr(0644,root,root)%{_unitdir}/zed-populator.service
%attr(0644,root,root)%{_unitdir}/device-scanner.target
%attr(0644,root,root)%{_unitdir}/device-scanner.socket
%attr(0644,root,root)%{_unitdir}/device-scanner.service
%attr(0644,root,root)%{_unitdir}/mount-emitter.service
%attr(0644,root,root)%{_unitdir}/mount-populator.service
%attr(0644,root,root)%{_unitdir}/swap-emitter.timer
%attr(0644,root,root)%{_unitdir}/swap-emitter.service
%attr(0644,root,root)%{_presetdir}/00-device-scanner.preset
%attr(0644,root,root)%{_sysconfdir}/udev/rules.d/99-iml-device-scanner.rules
%attr(0755,root,root)%{_bindir}/device-scanner-daemon
%attr(0755,root,root)%{_bindir}/uevent-listener
%attr(0755,root,root)%{_bindir}/mount-emitter


%post
systemctl preset device-scanner.socket
systemctl preset mount-emitter.service
systemctl preset swap-emitter.timer


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


%postun
%systemd_postun_with_restart device-scanner.socket


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