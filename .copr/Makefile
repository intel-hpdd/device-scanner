BUILDROOT:=$(shell [ -d "/build" ] && echo "/build" || echo ".")
TMPDIR:=$(shell mktemp -d)
CARGO := $(shell if which cargo 2>/dev/null; then true; else echo cargo; fi)

$(CARGO):
	yum install -y cargo

srpm: $(CARGO)
	mkdir -p ${TMPDIR}/_topdir/SOURCES
	mkdir -p ${TMPDIR}/_topdir/SPECS
	mkdir -p ${TMPDIR}/scratch

	rm -rf ${BUILDROOT}/_topdir

	if ! rpm -q dotnet-sdk-2.1 2> /dev/null; then \
		rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm; \
		yum-config-manager --add-repo=https://download.mono-project.com/repo/centos7-stable.repo; \
		yum install -y epel-release; \
		yum install -y mono-devel dotnet-sdk-2.1 nodejs nodejs-packaging; \
	fi

	cp -r /build/IML.CommonLibrary \
		/build/IML.DeviceAggregatorDaemon \
		/build/IML.DeviceScannerDaemon \
		/build/IML.IntegrationTest \
		/build/IML.IntegrationTestFramework \
		/build/IML.Listeners \
		/build/IML.ScannerProxyDaemon \
		/build/IML.StatefulPromise \
		/build/IML.Types \
		/build/multipath \
		/build/*.js \
		/build/device-scanner.sln \
		/build/iml-device-scanner.spec \
		/build/*.json \
		/build/paket.* \
		/build/Root.fsproj \
		/build/.paket ${TMPDIR}/scratch

	cd ${TMPDIR}/scratch && \
		npm i --ignore-scripts && \
		npm i iltorb && \
		npm run restore && \
		dotnet fable npm-run build && \
		npm pack;

	cp ${TMPDIR}/scratch/iml-device-scanner-*.tgz ${TMPDIR}/_topdir/SOURCES/
	cp iml-device-scanner.spec ${TMPDIR}/_topdir/SPECS/
	rpmbuild -bs -D "_topdir ${TMPDIR}/_topdir" ${TMPDIR}/_topdir/SPECS/iml-device-scanner.spec
	cp -rf ${TMPDIR}/_topdir ${BUILDROOT}/
	cp -f _topdir/SRPMS/*.rpm $(outdir)
