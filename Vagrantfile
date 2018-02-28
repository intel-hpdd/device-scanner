# -*- mode: ruby -*-
# vi: set ft=ruby :

$device_scanner_ip = "10.0.0.10"
$test_ip = "10.0.0.11"
$device_scanner_hostname = "devicescannernode"
$test_hostname = "testnode"
$public_key = IO.read('id_rsa.pub')

$hostedit = <<-SHELL
  sed -i '/.*devicescannernode$/d' /etc/hosts && echo "#{$device_scanner_ip} #{$device_scanner_hostname}" >> /etc/hosts
  sed -i '/.*testnode$/d' /etc/hosts && echo "#{$test_ip} #{$test_hostname}" >> /etc/hosts
SHELL

$setup_public_key = <<-SHELL
  echo '#{$public_key}' >> /root/.ssh/authorized_keys
SHELL

$set_key_permissions = <<-SHELL
  chmod 600 /root/.ssh/authorized_keys
  chmod 600 /root/.ssh/id_rsa
SHELL

Vagrant.configure("2") do |config|
  config.vm.box = "manager-for-lustre/centos74-1708-base"
  config.vm.synced_folder ".", "/vagrant", type: "virtualbox"
  config.vm.boot_timeout = 600
  config.ssh.username = 'root'
  config.ssh.password = 'vagrant'

   # Setup keys
   config.vm.provision "shell", inline: $hostedit
   config.vm.provision "file", source: "id_rsa", destination: "/root/.ssh/id_rsa"
   config.vm.provision "shell", inline: $setup_public_key
   config.vm.provision "shell", inline: $set_key_permissions

  #
  # Create a device-scanner node
  #
  config.vm.define "device-scanner", primary: true do |device_scanner|
    device_scanner.vm.provider "virtualbox" do |v|
      v.memory = 2048
      v.name = "device-scanner"

      file_to_disk = './tmp/device_scanner.vdi'

      v.customize ['setextradata', :id, 'VBoxInternal/Devices/ahci/0/Config/Port0/SerialNumber', '091118FC1221NCJ6G8GG']

      unless File.exist?(file_to_disk)
        v.customize ['createhd', '--filename', file_to_disk, '--size', 500 * 1024]
      end

      v.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', 1, '--device', 0, '--type', 'hdd', '--medium', file_to_disk]
      v.customize ['setextradata', :id, 'VBoxInternal/Devices/ahci/0/Config/Port1/SerialNumber', '081118FC1221NCJ6G8GG']
    end

    device_scanner.vm.hostname = $device_scanner_hostname
    device_scanner.vm.network "private_network", ip: $device_scanner_ip

    device_scanner.vm.provision "shell", inline: "cat >/root/.ssh/config<<__EOF
Host testnode
  Hostname #{$test_ip}
  StrictHostKeyChecking no
__EOF"

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
    npm run mock
    PACKAGE_VERSION=$(node -p -e "require('./package.json').version")
    RPM_NAME=iml-device-scanner-$PACKAGE_VERSION-2.el7.centos.x86_64.rpm
    docker cp mock:/var/lib/mock/epel-7-x86_64/result/$RPM_NAME ./
    yum install -y ./$RPM_NAME
    SHELL
  end

  #
  # Create a test node
  #
  config.vm.define "test", primary: false, autostart: false do |test|
    test.vm.provider "virtualbox" do |v|
      v.memory = 1024
      v.name = "test"
    end

    test.vm.hostname = $test_hostname
    test.vm.network "private_network", ip: $test_ip

    test.vm.provision "shell", inline: "cat >/root/.ssh/config<<__EOF
Host devicescannernode
  Hostname #{$device_scanner_ip}
  StrictHostKeyChecking no
__EOF"

    test.vm.provision "shell", inline: <<-SHELL
rpm --import "http://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
yum-config-manager --add-repo http://download.mono-project.com/repo/centos7/
yum install -y epel-release
yum install -y centos-release-dotnet
yum install -y nodejs mono-devel rh-dotnet20
rm -rf /builddir
cp -r /vagrant /builddir
cd /builddir
npm i --ignore-scripts
scl enable rh-dotnet20 "npm run restore"
scl enable rh-dotnet20 "dotnet fable npm-run integration-test"
cp /builddir/results.xml /vagrant
SHELL
  end
end
