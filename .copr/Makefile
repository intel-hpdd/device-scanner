srpm:
	curl https://sh.rustup.rs -sSf > ~/rustup.sh
	chmod 755 ~/rustup.sh
	~/rustup.sh -y
	source $(HOME)/.cargo/env
	mkdir -p /tmp/_topdir
	cd device-types
	cargo package --no-verify --allow-dirty
	cd ../device-scanner-daemon
	cargo package --no-verify --allow-dirty
	cd ../device-aggregator
	cargo package --no-verify --allow-dirty
	cd ../uevent-listener
	cargo package --no-verify --allow-dirty
	cd ../mount-emitter
	cargo package --no-verify --allow-dirty
	cd ../device-scanner-zedlets
	cargo package --no-verify --allow-dirty
	cd ../device-scanner-proxy
	cargo package --no-verify --allow-dirty
	cd ../futures-failure
	cargo package --no-verify --allow-dirty
	cd ../zed-enhancer
	cargo package --no-verify --allow-dirty
	cd ../
	mkdir -p /tmp/_topdir/SOURCES
	mv -f target/package/*.crate /tmp/_topdir/SOURCES/
	rpmbuild -bs --define "_topdir /tmp/_topdir" iml-device-scanner.spec
	cp /tmp/_topdir/SRPMS $(outdir)
