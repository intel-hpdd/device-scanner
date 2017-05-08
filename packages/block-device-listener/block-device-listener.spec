%{?nodejs_find_provides_and_requires}

Name:       block-device-listener
Version:    0.1.1
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

mkdir -p %{buildroot}%{nodejs_sitelib}/@mfl/block-device-listener
cp -pr lib package.json %{buildroot}%{nodejs_sitelib}/@mfl/block-device-listener

%nodejs_symlink_deps

%clean
rm -rf %{buildroot}

%files
%{nodejs_sitelib}/@mfl/block-device-listener

%changelog
* Mon May 8 2017 Joe Grund <grundjoseph@gmail.com> - 0.1.0
- initial package
