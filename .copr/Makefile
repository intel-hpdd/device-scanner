CARGO := $(shell if which cargo 2>/dev/null; then true; else echo cargo; fi)
BUILDROOT:=$(shell [ -d "/build" ] && echo "/build" || echo ".")
TMPDIR:=$(shell mktemp -d)

$(CARGO):
	yum install -y cargo

srpm: $(CARGO)
	mkdir -p ${TMPDIR}/_topdir/SOURCES
	mkdir -p ${TMPDIR}/_topdir/SPECS
	mkdir -p ${TMPDIR}/release/iml-device-scanner
	rm -rf ${BUILDROOT}/_topdir
	cargo build --release
	cp {device-aggregator,device-scanner-daemon,device-scanner-proxy,mount-emitter,zed-enhancer}/systemd-units/* \
		{zed-enhancer,uevent-listener}/udev-rules/* \
		target/release/device-{aggregator,scanner-daemon,scanner-proxy} \
		target/release/{history_event,pool_create,pool_destroy,pool_export,pool_import,vdev_add}-scanner \
		target/release/mount-emitter \
		target/release/uevent-listener \
		target/release/zed-enhancer \
		${TMPDIR}/release/iml-device-scanner
	tar -czvf ${TMPDIR}/_topdir/SOURCES/iml-device-scanner.tar.gz -C ${TMPDIR}/release/iml-device-scanner .
	cp iml-device-scanner.spec ${TMPDIR}/_topdir/SPECS/
	rpmbuild -bs -D "_topdir ${TMPDIR}/_topdir" ${TMPDIR}/_topdir/SPECS/iml-device-scanner.spec
	cp -rf ${TMPDIR}/_topdir ${BUILDROOT}/
	cp -f ${TMPDIR}/_topdir/SRPMS/*.rpm $(outdir)
