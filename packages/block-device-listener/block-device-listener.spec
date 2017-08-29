%{?nodejs_find_provides_and_requires}

Name:       block-device-listener
Version:    0.1.0
Release:    1%{?dist}
Summary:    Listens for block device add / removes. Writes to device-scanner-daemon socket.
License:    MIT
Group:      System Environment/Libraries
URL:        https://github.com/intel-hpdd/device-scanner/tree/master/packages/block-device-listener

Source0:    http://registry.npmjs.org/@mfl/block-device-listener/-/block-device-listener-0.1.0.tgz

BuildArch:  noarch
ExclusiveArch: %{nodejs_arches} noarch

BuildRequires:  nodejs-packaging

%description
A net.Socket client that is started when it receives a udev event for {ADD|REMOVE} of block devices.
The client will write to the unix domain socket of device-scanner-daemon
with the new event data. It acts as a middleman.
Udev event data is written over process.env to this module.

%prep
%setup -q -n package

%build
#nothing to do

%install
rm -rf %{buildroot}
mkdir -p $RPM_BUILD_ROOT/etc/udev/rules.d/
cp udev-rules/99-iml-device-scanner.rules $RPM_BUILD_ROOT/etc/udev/rules.d/99-iml-device-scanner.rules
mkdir -p $RPM_BUILD_ROOT/usr/lib/udev/
cp dist/block-device-listener $RPM_BUILD_ROOT/usr/lib/udev/block-device-listener

%clean
rm -rf %{buildroot}

%files
%attr(0744,root,root)/usr/lib/udev/block-device-listener
%attr(0744,root,root)/etc/udev/rules.d/99-iml-device-scanner.rules

%changelog
* Mon May 8 2017 Joe Grund <grundjoseph@gmail.com> - 0.1.0
- initial package
