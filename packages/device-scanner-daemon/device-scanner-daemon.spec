%{?nodejs_find_provides_and_requires}

Name:       device-scanner-daemon
Version:    0.1.0
Release:    1%{?dist}
Summary:    A persistent process that consumes udev events over a unix domain socket.
License:    MIT
Group:      System Environment/Libraries
URL:        https://github.com/intel-hpdd/device-scanner/tree/master/packages/device-scanner-daemon
Source0:    http://registry.npmjs.org/@mfl/device-scanner-daemon/-/device-scanner-daemon-0.1.0.tgz

BuildArch:  noarch
ExclusiveArch: %{nodejs_arches} noarch

BuildRequires:  nodejs-packaging

%description
A persistent process that consumes udev events over a unix domain socket.
There are two main modes to this daemon:
1. Processing new incoming events. In this mode we will munge and store incoming events.
2. Send current devices object listing. In this mode we will send our current stored devices.
We use unix domain sockets to communicate with the outside world.

%prep
%setup -q -n package

%build
#nothing to do

%install
rm -rf %{buildroot}

mkdir -p %{buildroot}%{nodejs_sitelib}/@mfl/device-scanner-daemon
cp -pr lib package.json %{buildroot}%{nodejs_sitelib}/@mfl/device-scanner-daemon

%nodejs_symlink_deps

%clean
rm -rf %{buildroot}

%files
%{nodejs_sitelib}/@mfl/device-scanner-daemon

%changelog
* Mon May 8 2017 Joe Grund <grundjoseph@gmail.com> - 0.1.0
- initial package
