CARGO := $(shell if which cargo 2>/dev/null; then true; else echo cargo; fi)
MONO := 

$(CARGO):
	dnf install -y cargo

srpm: $(CARGO)
	rpm --import "https://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
	su -c 'curl https://download.mono-project.com/repo/centos7-stable.repo | tee /etc/yum.repos.d/mono-centos7-stable.repo'
	curl https://raw.githubusercontent.com/creationix/nvm/v0.25.0/install.sh | bash && source $$HOME/.bash_profile && nvm install 8 && nvm use 8
	cd /tmp && curl https://sh.rustup.rs -o /tmp/rustup.rs && sh /tmp/rustup.rs -y && source $$HOME/.bash_profile && ls /build/.cargo/env && source /build/.cargo/env
	yum -y update
	yum groupinstall -y "Development tools"
	yum install -y mono-complete-4.0.5.1 
	yum install -y fsharp
	rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm
	yum install -y dotnet-sdk-2.2
	dotnet tool install fake-cli -g

	rm -rf /tmp/_topdir
	rm -rf /tmp/scratch
	mkdir -p /tmp/_topdir/SOURCES
	mkdir -p /tmp/scratch
	cp -r /build/IML.CommonLibrary /build/IML.DeviceAggregatorDaemon /build/IML.DeviceScannerDaemon /build/IML.IntegrationTest /build/IML.IntegrationTestFramework /build/IML.Listeners /build/IML.ScannerProxyDaemon /build/IML.StatefulPromise /build/IML.Types /build/multipath /build/*.js /build/*.fsx* /build/device-scanner.sln /build/iml-device-scanner.spec /build/*.json /build/paket.* /build/Root.fsproj /build/.paket /tmp/scratch
	source $$HOME/.bash_profile && cd /tmp/scratch && npm run restore && npm i --ignore-scripts && npm i iltorb && 
	
	mkdir -p /tmp/_topdir/SOURCES
	mv -f target/package/*.crate /tmp/_topdir/SOURCES/
	rpmbuild -bs --define "_topdir /tmp/_topdir" iml-device-scanner.spec
	cp -r /tmp/_topdir/SRPMS/* $(outdir)
