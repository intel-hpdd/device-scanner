srpm:

	rpm --import https://packages.microsoft.com/keys/microsoft.asc
	rpm -Uvh https://packages.microsoft.com/config/fedora/27/prod.repo
	dnf repolist
	dnf search dotnet
	dnf -y -v install nodejs aspnetcore-runtime-2.1
	curl -o /tmp/cacert.pem https://curl.haxx.se/ca/cacert.pem
	cert-sync --user /tmp/cacert.pem
	npm i --ignore-scripts
	export DOTNET_CLI_TELEMETRY_OPTOUT=1
	npm run restore
	dotnet fable npm-build
	npm pack

	rpmbuild -bs --define epel\ 1 --define _srcrpmdir\ ${CURDIR} --define _sourcedir\ ${CURDIR} --define _rpmdir\ ${outdir} ${spec}
