# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  config.vm.box = "manager-for-lustre/centos74-1708-base"
  config.vm.synced_folder ".", "/vagrant", type: "virtualbox"

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
    yum install -y epel-release yum-plugin-copr http://download.zfsonlinux.org/epel/zfs-release.el7_4.noarch.rpm
    yum -y copr enable alonid/llvm-5.0.0
    yum install -y nodejs socat jq clang-5.0.0 zfs libzfs2-devel
    cd /vagrant
    mkdir -p /usr/lib64/iml-device-scanner-daemon
    cp dist/device-scanner-daemon/device-scanner-daemon /usr/lib64/iml-device-scanner-daemon
    cp dist/event-listener/event-listener /lib/udev
    chmod 777 /lib/udev/event-listener
    cp dist/event-listener/event-listener /usr/libexec/zfs/zed.d/generic-listener.sh
    chmod 755 /usr/libexec/zfs/zed.d/generic-listener.sh
    ln -sf /usr/libexec/zfs/zed.d/generic-listener.sh /etc/zfs/zed.d/pool_create-scanner.sh 
    ln -sf /usr/libexec/zfs/zed.d/generic-listener.sh /etc/zfs/zed.d/pool_destroy-scanner.sh 
    ln -sf /usr/libexec/zfs/zed.d/generic-listener.sh /etc/zfs/zed.d/pool_import-scanner.sh 
    ln -sf /usr/libexec/zfs/zed.d/generic-listener.sh /etc/zfs/zed.d/pool_export-scanner.sh 
    ln -sf /usr/libexec/zfs/zed.d/generic-listener.sh /etc/zfs/zed.d/history_event-scanner.sh 
    cp dist/event-listener/IML/EventListener/udev-rules/99-iml-device-scanner.rules /etc/udev/rules.d/
    cp dist/device-scanner-daemon/IML/DeviceScannerDaemon/systemd-units/* /usr/lib/systemd/system
    systemctl enable device-scanner.socket
    systemctl start device-scanner.socket
    udevadm trigger --action=change --subsystem-match=block
    /sbin/modprobe zfs
    systemctl enable zfs-zed.service
    systemctl start zfs-zed.service
  SHELL
end
