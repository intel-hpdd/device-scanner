# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  config.vm.box = "manager-for-lustre/centos7-dotnet-node"

  config.vm.provider "virtualbox" do |vb|
    vb.memory = "1024"

    file_to_disk = './tmp/large_disk.vdi'

    unless File.exist?(file_to_disk)
      vb.customize ['createhd', '--filename', file_to_disk, '--size', 500 * 1024]
    end

    vb.customize ['storagectl', :id, '--name', 'SCSI', '--add', 'scsi']
    vb.customize ['storageattach', :id, '--storagectl', 'SCSI', '--port', 1, '--device', 0, '--type', 'hdd', '--medium', file_to_disk]
  end

  config.vm.boot_timeout = 600

  config.vm.provision "shell", inline: <<-SHELL
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
