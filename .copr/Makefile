CARGO := $(shell if which cargo 2>/dev/null; then true; else echo cargo; fi)

$(CARGO):
	dnf install -y cargo

srpm: $(CARGO)
	rpm --import "https://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
	su -c 'curl https://download.mono-project.com/repo/centos7-stable.repo | tee /etc/yum.repos.d/mono-centos7-stable.repo'
	yum update
	yum install -y epel-release
	curl https://raw.githubusercontent.com/creationix/nvm/v0.25.0/install.sh | bash && source $HOME/.bash_profile && nvm use 8act
	cd /tmp && curl https://sh.rustup.rs -o /tmp/rustup.rs && sh /tmp/rustup.rs -y && source $HOME/.cargo/env
	yum groupinstall "Development tools"
	yum install -y mono-complete-4.0.5.1 fsharp
	rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm
	yum install -y dotnet-sdk-2.2
	dotnet tool install fake-cli -g

	rm -rf /tmp/_topdir
	rm -rf /tmp/scratch
	mkdir -p /tmp/_topdir/SOURCES
	mkdir -p /tmp/scratch
	cp -r /build/IML.CommonLibrary /build/IML.DeviceAggregatorDaemon /build/IML.DeviceScannerDaemon /build/IML.IntegrationTest /build/IML.IntegrationTestFramework /build/IML.Listeners /build/IML.ScannerProxyDaemon /build/IML.StatefulPromise /build/IML.Types multipath /build/*.js /build/*.fsx* /build/device-scanner.sln /build/iml-device-scanner.spec /build/*.json /build/paket.* /build/Root.fsproj /tmp/scratch
	cd /build && npm i && fake run build.fsx
	
	mkdir -p /tmp/_topdir/SOURCES
	mv -f target/package/*.crate /tmp/_topdir/SOURCES/
	rpmbuild -bs --define "_topdir /tmp/_topdir" iml-device-scanner.spec
	cp -r /tmp/_topdir/SRPMS/* $(outdir)
