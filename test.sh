su -i
echo "Installing centos-release-scl"
yum -y install centos-release-scl
echo "Running dotnet-setup.sh"
./dotnet-setup.sh
echo "running dotnet restore and npm-build"
scl enable rh-dotnet20 "npm run restore && dotnet fable npm-build"
echo "bringing vagrant up"
vagrant up
echo "running integration tests"
scl enable rh-dotnet20 "dotnet fable npm-run integration-test"
echo "destroying vagrant node"
vagrant destroy -f
