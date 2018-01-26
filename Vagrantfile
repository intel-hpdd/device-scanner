# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  config.vm.box = "manager-for-lustre/centos74-1708-base"
  config.vm.synced_folder ".", "/vagrant", type: "virtualbox"
  config.vm.boot_timeout = 600

  config.ssh.username = 'root'
  config.ssh.password = 'vagrant'
  config.ssh.insert_key = 'true'

  # Create a set of /24 networks under a single /16 subnet range
	subnet_prefix="10.73"
	# Management network for admin comms
  mgmt_net_pfx="#{subnet_prefix}.10"

  # Create a basic hosts file for the VMs.
	open('hosts', 'w') { |f|
  f.puts <<-__EOF
127.0.0.1   localhost localhost.localdomain localhost4 localhost4.localdomain4
::1         localhost localhost.localdomain localhost6 localhost6.localdomain6
#{mgmt_net_pfx}.10 test.lfs.local testnode
#{mgmt_net_pfx}.11 devicescanner.lfs.local devicescannernode
__EOF
	}
	config.vm.provision "shell", inline: "cp -f /vagrant/hosts /etc/hosts"
	config.vm.provision "shell", inline: "selinuxenabled && setenforce 0; cat >/etc/selinux/config<<__EOF
SELINUX=disabled
SELINUXTYPE=targeted
__EOF"

  # A simple way to create a key that can be used to enable
	# SSH between the virtual guests.
	#
	# The private key is copied onto the root account of the
	# administration node and the public key is appended to the
	# authorized_keys file of the root account for all nodes
	# in the cluster.
	#
	# Shelling out may not be the most Vagrant-friendly means to
	# create this key but it avoids more complex methods such as
	# developing a plugin.
	#
	# Popen may be a more secure way to exec but is more code
	# for what is, in this case, a relatively small gain.
	if not(File.exist?("id_rsa"))
		res = system("ssh-keygen -t rsa -N '' -f id_rsa")
  end

  config.vm.provision "shell", inline: "mkdir -m 0700 -p /root/.ssh; [ -f /vagrant/id_rsa.pub ] && (awk -v pk=\"`cat /vagrant/id_rsa.pub`\" 'BEGIN{split(pk,s,\" \")} $2 == s[2] {m=1;exit}END{if (m==0)print pk}' /root/.ssh/authorized_keys )>> /root/.ssh/authorized_keys; chmod 0600 /root/.ssh/authorized_keys"


  #
  # Create a device-scanner node
  #
  config.vm.define "device-scanner", primary: true do |device_scanner|
    device_scanner.vm.provider "virtualbox" do |v|
      v.memory = 1024
      v.name = "device-scanner"

      file_to_disk = './tmp/device_scanner.vdi'

      v.customize ['setextradata', :id,
  'VBoxInternal/Devices/ahci/0/Config/Port0/SerialNumber', '091118FC1221NCJ6G8GG']

      unless File.exist?(file_to_disk)
        v.customize ['createhd', '--filename', file_to_disk, '--size', 500 * 1024]
      end

      v.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', 1, '--device', 0, '--type', 'hdd', '--medium', file_to_disk]
      v.customize ['setextradata', :id,
  'VBoxInternal/Devices/ahci/0/Config/Port1/SerialNumber', '081118FC1221NCJ6G8GG']
    end

    device_scanner.vm.provision "shell", inline: <<-SHELL
    rpm --import "http://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
    yum-config-manager --add-repo http://download.mono-project.com/repo/centos7/
    wget https://bintray.com/intel-hpdd/intelhpdd-build/rpm -O /etc/yum.repos.d/bintray-intel-hpdd-intelhpdd-build.repo
    yum install -y epel-release http://download.zfsonlinux.org/epel/zfs-release.el7_4.noarch.rpm
    yum install -y centos-release-dotnet
    yum install -y nodejs socat jq docker mono-devel rh-dotnet20 git
    systemctl start docker
    docker rm mock -f
    rm -rf /builddir
    cp -r /vagrant /builddir
    cd /builddir
    npm i --ignore-scripts
    scl enable rh-dotnet20 "npm run restore && dotnet fable npm-build"
    npm pack
    rename 'iml-' '' iml-device-scanner-*.tgz
    npm run mock
    PACKAGE_VERSION=$(node -p -e "require('./package.json').version")
    RELEASE=$(git rev-list HEAD | wc -l)
    RPM_NAME=iml-device-scanner2-$PACKAGE_VERSION-$RELEASE.el7.centos.x86_64.rpm
    docker cp mock:/var/lib/mock/epel-7-x86_64/result/$RPM_NAME ./
    yum install -y ./$RPM_NAME
    SHELL

    device_scanner.vm.host_name = "devicescanner.lfs.local"
    device_scanner.vm.network "private_network",
      ip: "#{mgmt_net_pfx}.10",
      netmask: "255.255.255.0"
  end

  #
  # Create a test node
  #
  config.vm.define "test", primary: false do |test|
    test.vm.provider "virtualbox" do |v|
      v.memory = 1024
      v.name = "test"
    end

    test.vm.host_name = "test.lfs.local"
    test.vm.network "private_network",
      ip: "#{mgmt_net_pfx}.11",
      netmask: "255.255.255.0"

    test.vm.provision "shell", inline: <<-SHELL
rpm --import "http://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
yum-config-manager --add-repo http://download.mono-project.com/repo/centos7/
yum install -y epel-release
yum install -y centos-release-dotnet
yum install -y nodejs mono-devel rh-dotnet20
rm -rf /builddir
cp -r /vagrant /builddir
cd /builddir
rm -rf node_modules bin obj dist
npm i --ignore-scripts
scl enable rh-dotnet20 "npm run restore && dotnet fable npm-build"
scl enable rh-dotnet20 "dotnet fable npm-run integration-test"
SHELL
  end
end
