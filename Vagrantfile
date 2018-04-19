# -*- mode: ruby -*-
# vi: set ft=ruby :

$suffix = ENV['NAME_SUFFIX'] or ""

$device_scanner_hostname = "devicescannernode#{$suffix}"
$test_hostname = "testnode#{$suffix}"
$public_key = IO.read('id_rsa.pub')
$get_enp0s8_ip = "ip a | grep 'enp0s8$' | awk '{print $2}' | sed -e 's@\/[0-9]*@@'"
$get_enp0s9_ip = "ip a | grep 'enp0s9$' | awk '{print $2}' | sed -e 's@\/[0-9]*@@'"
$get_enp0s10_ip = "ip a | grep 'enp0s10$' | awk '{print $2}' | sed -e 's@\/[0-9]*@@'"

$get_device_scanner_ip = "grep \"devicescannernode\" /vagrant/hosts | awk '{print $1}'"
$get_device_scanner_host_entry = "grep devicescannernode /vagrant/hosts"
$get_test_ip = "grep \"testnode\" /vagrant/hosts | awk '{print $1}'"
$get_test_host_entry = "grep \"testnode\" /vagrant/hosts"

$setup_public_key = <<-SHELL
  echo "#{$public_key}" >> /root/.ssh/authorized_keys
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
   config.vm.provision "file", source: "id_rsa", destination: "/root/.ssh/id_rsa"
   config.vm.provision "shell", inline: $setup_public_key
   config.vm.provision "shell", inline: $set_key_permissions

  #
  # Create a device-scanner node
  #
  config.vm.define "device-scanner#{$suffix}", primary: true do |device_scanner|
    device_scanner.vm.provider "virtualbox" do |v|
      v.memory = 2048
      v.cpus = 4
      v.name = "device-scanner#{$suffix}"

      disk1 = './tmp/disk1.vdi'
      unless File.exist?(disk1)
        v.customize ['createhd', '--filename', disk1, '--size', 500 * 1024]
      end

      v.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', 1, '--device', 0, '--type', 'hdd', '--medium', disk1]
      v.customize ['setextradata', :id, 'VBoxInternal/Devices/ahci/0/Config/Port0/SerialNumber', '081118FC1221NCJ6G801']
      v.customize ['setextradata', :id, 'VBoxInternal/Devices/ahci/0/Config/Port1/SerialNumber', '081118FC1221NCJ6G830']

      for i in 2..29 do
        id = i.to_s.rjust(2, '0')
        disk = "./tmp/disk#{i}.vdi"

        unless File.exist?(disk)
          v.customize ["createmedium", "disk",
            "--filename", disk,
            "--size", "100",
            "--format", "VDI",
            "--variant", "fixed"
          ]
        end

        v.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', i, '--type', 'hdd', '--medium', disk]
        v.customize ['setextradata', :id, "VBoxInternal/Devices/ahci/0/Config/Port#{i}/SerialNumber", "081118FC1221NCJ6G8#{id}"]
      end
    end

    device_scanner.vm.hostname = $device_scanner_hostname
    device_scanner.vm.network "private_network", type: "dhcp"
    device_scanner.vm.network :forwarded_port, host: 8080, guest: 8080, auto_correct: true

    device_scanner.vm.provision "shell", inline: <<-SHELL
    touch /vagrant/hosts
    sed -i '/devicescannernode.*$/d' /vagrant/hosts
    echo "$(#{$get_enp0s8_ip}) $(hostname)" >> /vagrant/hosts
    SHELL

    device_scanner.vm.provision "shell", inline: <<-SHELL
    yum install -y epel-release http://download.zfsonlinux.org/epel/zfs-release.el7_4.noarch.rpm yum-plugin-copr
    yum install -y nodejs socat jq docker git device-mapper-multipath iscsi-initiator-utils
    yum -y copr enable managerforlustre/manager-for-lustre
    systemctl start docker
    docker rm mock -f
    rm -rf /builddir
    cp -r /vagrant /builddir
    cd /builddir
    npm run mock
    PACKAGE_VERSION=$(node -p -e "require('./package.json').version")
    RPM_NAME=iml-device-scanner-$PACKAGE_VERSION-2.el7.centos.x86_64.rpm
    docker cp mock:/var/lib/mock/epel-7-x86_64/result/$RPM_NAME ./
    docker rm -f mock
    systemctl stop docker
    yum install -y ./$RPM_NAME
    systemctl stop docker
    cp /vagrant/multipath/multipath.conf /etc
    systemctl enable multipathd
    systemctl start multipathd
    echo "InitiatorName=iqn.2018-03.com.test:client" > /etc/iscsi/initiatorname.iscsi
    systemctl start iscsid
    systemctl enable iscsid
    systemctl start iscsi
    systemctl enable iscsi
    SHELL
  end

  #
  # Create a test node
  #
  config.vm.define "test#{$suffix}", primary: false do |test|
    test.vm.provider "virtualbox" do |v|
      v.memory = 1024
      v.cpus = 2
      v.name = "test#{$suffix}"

      disk1 = './tmp/test0.vdi'
      unless File.exist?(disk1)
        v.customize ['createhd', '--filename', disk1, '--size', 2 * 1024]
      end

      v.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', 1, '--device', 0, '--type', 'hdd', '--medium', disk1]
      v.customize ['setextradata', :id, 'VBoxInternal/Devices/ahci/0/Config/Port0/SerialNumber', '081118FC1223NCC281F0']

      for i in 1..2 do
        id = i.to_s.rjust(2, '0')
        disk = "./tmp/test#{i}.vdi"

        unless File.exist?(disk)
          v.customize ["createmedium", "disk",
            "--filename", disk,
            "--size", "100",
            "--format", "VDI",
            "--variant", "fixed"
          ]
        end

        v.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', i, '--type', 'hdd', '--medium', disk]
        v.customize ['setextradata', :id, "VBoxInternal/Devices/ahci/0/Config/Port#{i}/SerialNumber", "081118FC1223NCC281#{id}"]
      end
    end

    test.vm.hostname = $test_hostname
    test.vm.network "private_network", type: "dhcp"
    test.vm.network "private_network", type: "dhcp"
    test.vm.network "private_network", type: "dhcp"

    test.vm.provision "shell", inline: <<-SHELL
    touch /vagrant/hosts
    sed -i '/testnode.*$/d' /vagrant/hosts
    echo "$(#{$get_enp0s8_ip}) $(hostname)" >> /vagrant/hosts
    sed -i '/TEST_INTERFACE_1.*$/d' ~/.bashrc
    echo "export TEST_INTERFACE_1=$(#{$get_enp0s8_ip})" >> ~/.bashrc
    sed -i '/TEST_INTERFACE_2.*$/d' ~/.bashrc
    echo "export TEST_INTERFACE_2=$(#{$get_enp0s9_ip})" >> ~/.bashrc
    sed -i '/TEST_INTERFACE_3.*$/d' ~/.bashrc
    echo "export TEST_INTERFACE_3=$(#{$get_enp0s10_ip})" >> ~/.bashrc
    SHELL

    test.vm.provision "shell", inline: <<-SHELL
device_scanner_node_ip=$(#{$get_device_scanner_ip})
test_node_ip=$(#{$get_test_ip})
sed -i '/.*devicescannernode/d' /etc/hosts && echo "$(#{$get_device_scanner_host_entry})" >> /etc/hosts
sed -i '/.*testnode/d' /etc/hosts && echo "$(#{$get_test_host_entry})" >> /etc/hosts

cat >/root/.ssh/config<<__EOF
Host devicescannernode
  Hostname $device_scanner_node_ip
  StrictHostKeyChecking no

Host #{$device_scanner_hostname}
  Hostname $device_scanner_node_ip
  StrictHostKeyChecking no
__EOF

ssh #{$device_scanner_hostname} "
sed -i '/.*devicescannernode/d' /etc/hosts && echo "$(#{$get_device_scanner_host_entry})" >> /etc/hosts
sed -i '/.*testnode/d' /etc/hosts && echo "$(#{$get_test_host_entry})" >> /etc/hosts

cat >/root/.ssh/config<<__EOF
Host testnode
  Hostname $test_node_ip
  StrictHostKeyChecking no

Host #{$test_hostname}
  Hostname $test_node_ip
  StrictHostKeyChecking no
__EOF
"
SHELL

    test.vm.provision "shell", inline: <<-SHELL
rpm --import "http://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
yum-config-manager --add-repo http://download.mono-project.com/repo/centos7/
yum install -y epel-release
yum install -y centos-release-dotnet
yum install -y nodejs mono-devel rh-dotnet20
yum install -y targetcli
cp /vagrant/multipath/saveconfig.json /etc/target/
pvcreate /dev/sdb /dev/sdc
vgcreate iscsivg /dev/sdc; lvcreate -l100%FREE -n iscsilv iscsivg
systemctl start target.service
systemctl enable target.service
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
