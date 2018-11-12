srpm:
	curl https://sh.rustup.rs -sSf > ~/rustup.sh
	chmod 755 ~/rustup.sh
	~/rustup.sh -y
	mkdir -p /tmp/_topdir
	cd device-types
	$(HOME)/.cargo/bin package --no-verify --allow-dirty
	cd ../device-scanner-daemon
	$(HOME)/.cargo/bin package --no-verify --allow-dirty
	cd ../device-aggregator
	$(HOME)/.cargo/bin package --no-verify --allow-dirty
	cd ../uevent-listener
	$(HOME)/.cargo/bin package --no-verify --allow-dirty
	cd ../mount-emitter
	$(HOME)/.cargo/bin package --no-verify --allow-dirty
	cd ../device-scanner-zedlets
	$(HOME)/.cargo/bin package --no-verify --allow-dirty
	cd ../device-scanner-proxy
	$(HOME)/.cargo/bin package --no-verify --allow-dirty
	cd ../futures-failure
	$(HOME)/.cargo/bin package --no-verify --allow-dirty
	cd ../zed-enhancer
	$(HOME)/.cargo/bin package --no-verify --allow-dirty
	cd ../
	mkdir -p /tmp/_topdir/SOURCES
	mv -f target/package/*.crate /tmp/_topdir/SOURCES/
	rpmbuild -bs --define "_topdir /tmp/_topdir" iml-device-scanner.spec
	cp /tmp/_topdir/SRPMS $(outdir)
