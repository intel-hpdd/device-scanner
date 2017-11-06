# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  config.vm.box = "manager-for-lustre/centos74-1708-base"

  config.vm.provider "virtualbox" do |vb|
    vb.memory = "1024"
    vb.name = "device-scanner"

    file_to_disk = './tmp/large_disk.vdi'

    vb.customize ['setextradata', :id,
'VBoxInternal/Devices/ahci/0/Config/Port0/SerialNumber', '091118FC1221NCJ6G8GG']

    unless File.exist?(file_to_disk)
      vb.customize ['createhd', '--filename', file_to_disk, '--size', 500 * 1024]
    end

    vb.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', 1, '--device', 0, '--type', 'hdd', '--medium', file_to_disk]
    vb.customize ['setextradata', :id,
'VBoxInternal/Devices/ahci/0/Config/Port1/SerialNumber', '081118FC1221NCJ6G8GG']
  end

  config.vm.boot_timeout = 600

  config.vm.provision "shell", inline: <<-SHELL
    yum install -y epel-release
    yum install -y nodejs socat jq
    cd /vagrant
    mkdir -p /usr/lib64/iml-device-scanner-daemon
    cp dist/device-scanner-daemon/device-scanner-daemon /usr/lib64/iml-device-scanner-daemon
    cp dist/block-device-listener/block-device-listener /lib/udev
    chmod 777 /lib/udev/block-device-listener
    cp dist/block-device-listener/udev-rules/99-iml-device-scanner.rules /etc/udev/rules.d/
    cp dist/device-scanner-daemon/systemd-units/* /usr/lib/systemd/system
    systemctl enable device-scanner.socket
    systemctl start device-scanner.socket
    udevadm trigger --action=change --subsystem-match=block
  SHELL
end
