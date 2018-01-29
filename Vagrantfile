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
    rpm --import "http://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
    yum-config-manager --add-repo http://download.mono-project.com/repo/centos7/
    wget https://bintray.com/intel-hpdd/intelhpdd-build/rpm -O /etc/yum.repos.d/bintray-intel-hpdd-intelhpdd-build.repo
    yum install -y epel-release http://download.zfsonlinux.org/epel/zfs-release.el7_4.noarch.rpm
    yum install -y centos-release-dotnet
    yum install -y nodejs socat jq docker mono-devel rh-dotnet20
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
    docker cp mock:/var/lib/mock/epel-7-x86_64/result/iml-device-scanner2-2.0.0-76.el7.centos.x86_64.rpm ./
    yum install -y ./iml-device-scanner2-2.0.0-*.el7.centos.x86_64.rpm
  SHELL
end
