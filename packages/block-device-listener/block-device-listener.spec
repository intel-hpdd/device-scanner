%{?nodejs_find_provides_and_requires}

Name:       block-device-listener
Version:    0.1.0
Summary:    Listens for block device add / removes. Writes to device-scanner-daemon socket.
License:    MIT
Group:      System Environment/Libraries
URL:        https://github.com/intel-hpdd/device-scanner/tree/master/packages/block-device-listener

Source0:    http://registry.npmjs.org/@mfl/block-device-listener/-/block-device-listener-0.1.0.tgz

BuildArch:  noarch
ExclusiveArch: %{nodejs_arches} noarch

BuildRequires:  nodejs-packaging

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
* Mon May 8th 2017 Joe Grund <grundjoseph@gmail.com> - 0.1.0
- initial package
