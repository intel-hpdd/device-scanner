# -*- mode: ruby -*-
# vi: set ft=ruby :

NAME_SUFFIX = (ENV['NAME_SUFFIX'] || '').freeze

def provision_mdns(config)
  config.vm.provision 'mdns', type: 'shell', inline: <<-SHELL
    yum install -y avahi nss-mdns
    sed -i 's/myhostname/mdns/' /etc/nsswitch.conf
    systemctl restart network
    systemctl enable avahi-daemon.socket
    systemctl start avahi-daemon.socket
    systemctl start avahi-daemon.service
    systemctl status avahi-daemon.service
  SHELL
end

Vagrant.configure('2') do |config|
  config.vm.box = 'manager-for-lustre/centos75-1804-device-scanner'
  config.vm.box_version = '0.0.5'

  INT_NET_NAME = "scanner-net#{NAME_SUFFIX}".freeze

  system("ssh-keygen -t rsa -N '' -f id_rsa") unless File.exist?('id_rsa')

  config.vm.provision 'ssh', type: 'shell', inline: <<-SHELL
    mkdir -m 0700 -p /root/.ssh
    cp /vagrant/id_rsa /root/.ssh/.
    chmod 0600 /root/.ssh/id_rsa
    mkdir -m 0700 -p /root/.ssh
    [ -f /vagrant/id_rsa.pub ] && (awk -v pk=\"`cat /vagrant/id_rsa.pub`\" 'BEGIN{split(pk,s,\" \")} $2 == s[2] {m=1;exit}END{if (m==0)print pk}' /root/.ssh/authorized_keys ) >> /root/.ssh/authorized_keys
    chmod 0600 /root/.ssh/authorized_keys

    cat > /etc/ssh/ssh_config <<__EOF
    Host *
        StrictHostKeyChecking no
__EOF
  SHELL

  # Create device-scanner node
  SCANNER_NAME = 'device-scanner'.freeze
  config.vm.define "#{SCANNER_NAME}#{NAME_SUFFIX}", primary: true do |device_scanner|
    device_scanner.vm.hostname = SCANNER_NAME
    device_scanner.ssh.username = 'root'
    device_scanner.ssh.password = 'vagrant'
    device_scanner.vm.network :forwarded_port,
                              host: 8080,
                              guest: 8080,
                              auto_correct: true
    device_scanner.vm.network 'private_network',
                              ip: '10.0.0.10',
                              virtualbox__intnet: INT_NET_NAME

    device_scanner.vm.provider 'virtualbox' do |v|
      v.memory = 2048
      v.cpus = 4
      v.name = "#{SCANNER_NAME}#{NAME_SUFFIX}"

      disk1 = './tmp/disk1.vdi'
      unless File.exist?(disk1)
        v.customize ['createhd', '--filename', disk1, '--size', 500 * 1024]
      end

      v.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', 1, '--device', 0, '--type', 'hdd', '--medium', disk1]
      v.customize ['setextradata', :id, 'VBoxInternal/Devices/ahci/0/Config/Port0/SerialNumber', '081118FC1221NCJ6G801']
      v.customize ['setextradata', :id, 'VBoxInternal/Devices/ahci/0/Config/Port1/SerialNumber', '081118FC1221NCJ6G830']

      (2..29).each do |i|
        id = i.to_s.rjust(2, '0')
        disk = "./tmp/disk#{i}.vdi"

        unless File.exist?(disk)
          v.customize ['createmedium', 'disk',
                       '--filename', disk,
                       '--size', '100',
                       '--format', 'VDI',
                       '--variant', 'fixed']
        end

        v.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', i, '--type', 'hdd', '--medium', disk]
        v.customize ['setextradata', :id, "VBoxInternal/Devices/ahci/0/Config/Port#{i}/SerialNumber", "081118FC1221NCJ6G8#{id}"]
      end
    end

    device_scanner.vm.provision 'deps', type: 'shell', inline: <<-SHELL
      yum install -y http://download.zfsonlinux.org/epel/zfs-release.el7_5.noarch.rpm
    SHELL

    device_scanner.vm.provision 'build', type: 'shell', inline: <<-SHELL
      rm -rf /builddir
      cp -r /vagrant /builddir
    SHELL

    device_scanner.vm.provision 'install', type: 'shell', inline: <<-SHELL
      cd /builddir
      ./mock-build.sh
      yum install -y iml-device-scanner-proxy*.x86_64.rpm
      cp /builddir/iml-device-scanner-aggregator*.x86_64.rpm /vagrant
    SHELL

    device_scanner.vm.provision 'mpath', type: 'shell', inline: <<-SHELL
      cp /vagrant/multipath/multipath.conf /etc
      systemctl enable multipathd
      systemctl start multipathd
      echo "InitiatorName=iqn.2018-03.com.test:client" > /etc/iscsi/initiatorname.iscsi
      systemctl start iscsi
      systemctl enable iscsi
    SHELL

    device_scanner.vm.provision 'certs', type: 'shell', inline: <<-SHELL
      mkdir -p /etc/iml
      cd /etc/iml
      openssl req \
        -subj '/CN=managernode.com/O=Intel/C=US' \
        -newkey rsa:2048 -nodes -keyout manager.key \
        -x509 -days 365 -out manager.crt
      openssl dhparam -out manager.pem 2048
      mkdir -p /vagrant/certs
      cp /etc/iml/manager.crt /vagrant/certs
      cp /etc/iml/manager.key /vagrant/certs
      cp /etc/iml/manager.pem /vagrant/certs
    SHELL

    device_scanner.vm.provision 'manager-conf', type: 'shell', inline: <<-SHELL
      mkdir -p /etc/iml
      touch /etc/iml/manager-url.conf
      echo "IML_MANAGER_URL=https://10.0.0.20:443/iml-device-aggregator" > /etc/iml/manager-url.conf
      echo "IML_CERT_PATH=/etc/iml/manager.crt" >> ~/.bashrc
      echo "IML_PRIVATE_KEY=/etc/iml/chroma/manager.key" >> ~/.bashrc
    SHELL

    provision_mdns device_scanner
  end

  # Create the manager node
  MANAGER_NAME = 'manager'.freeze
  config.vm.define "#{MANAGER_NAME}#{NAME_SUFFIX}" do |manager|
    manager.vm.hostname = MANAGER_NAME
    manager.ssh.username = 'root'
    manager.ssh.password = 'vagrant'
    manager.vm.network 'private_network',
                              ip: '10.0.0.20',
                              virtualbox__intnet: INT_NET_NAME

    manager.vm.provider 'virtualbox' do |v|
      v.memory = 512
      v.cpus = 1
      v.name = "#{MANAGER_NAME}#{NAME_SUFFIX}"
    end

    manager.vm.provision 'deps', type: 'shell', inline: <<-SHELL
      yum install -y /vagrant/iml-device-scanner-aggregator*.x86_64.rpm
      yum install -y nginx
    SHELL

    manager.vm.provision 'certs', type: 'shell', inline: <<-SHELL
      mkdir -p /etc/iml
      cp /vagrant/certs/manager.crt /etc/iml
      cp /vagrant/certs/manager.key /etc/iml
      cp /vagrant/certs/manager.pem /etc/iml
    SHELL

    manager.vm.provision 'nginx', type: 'shell', inline: <<-SHELL
      cp /vagrant/nginx/manager-proxy.conf /etc/nginx/conf.d
      systemctl reload nginx
    SHELL

    provision_mdns manager
  end

  # Create test node
  TEST_NAME = 'test'.freeze
  config.vm.define "#{TEST_NAME}#{NAME_SUFFIX}" do |test|
    test.vm.hostname = TEST_NAME
    test.ssh.username = 'root'
    test.ssh.password = 'vagrant'

    (30..50).step(10).each do |i|
      test.vm.network 'private_network',
                      ip: "10.0.0.#{i}",
                      virtualbox__intnet: INT_NET_NAME
    end

    test.vm.provider 'virtualbox' do |v|
      v.memory = 1024
      v.cpus = 2
      v.name = "#{TEST_NAME}#{NAME_SUFFIX}"

      disk1 = './tmp/test0.vdi'
      unless File.exist?(disk1)
        v.customize ['createhd', '--filename', disk1, '--size', 2 * 1024]
      end

      v.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', 1, '--device', 0, '--type', 'hdd', '--medium', disk1]
      v.customize ['setextradata', :id, 'VBoxInternal/Devices/ahci/0/Config/Port0/SerialNumber', '081118FC1223NCC281F0']

      (1..2).each do |i|
        id = i.to_s.rjust(2, '0')
        disk = "./tmp/test#{i}.vdi"

        unless File.exist?(disk)
          v.customize ['createmedium', 'disk',
                       '--filename', disk,
                       '--size', '100',
                       '--format', 'VDI',
                       '--variant', 'fixed']
        end

        v.customize ['storageattach', :id, '--storagectl', 'SATA Controller', '--port', i, '--type', 'hdd', '--medium', disk]
        v.customize ['setextradata', :id, "VBoxInternal/Devices/ahci/0/Config/Port#{i}/SerialNumber", "081118FC1223NCC281#{id}"]
      end
    end

    test.vm.provision 'configure', type: 'shell', inline: <<-SHELL
      echo "export IML_MANAGER_URL=https://#{$manager_hostname}:443/iml-device-aggregator" >> ~/.bashrc
      echo "export IML_CERT_PATH=/etc/iml/manager.crt" >> ~/.bashrc
      echo "export IML_PRIVATE_KEY=/etc/iml/manager.key" >> ~/.bashrc
    SHELL

    test.vm.provision 'deps', type: 'shell', inline: <<-SHELL
      yum install -y targetcli
    SHELL

    test.vm.provision 'certs', type: 'shell', inline: <<-SHELL
      mkdir -p /etc/iml
      cp /vagrant/certs/manager.crt /etc/iml
      cp /vagrant/certs/manager.key /etc/iml
      cp /vagrant/certs/manager.pem /etc/iml
    SHELL

    test.vm.provision 'devices', type: 'shell', inline: <<-SHELL
      cp /vagrant/multipath/saveconfig.json /etc/target/
      pvcreate /dev/sdb /dev/sdc
      vgcreate iscsivg /dev/sdc; lvcreate -l100%FREE -n iscsilv iscsivg
      systemctl start target.service
      systemctl enable target.service
    SHELL

    provision_mdns test

    test.vm.provision 'install', type: 'shell', inline: <<-SHELL
      rm -rf /builddir
      cp -r /vagrant /builddir
      cd /builddir
      npm i --ignore-scripts
      cert-sync /etc/pki/tls/certs/ca-bundle.crt
      npm run restore
    SHELL

    test.vm.provision 'integration-test', type: 'shell', inline: <<-SHELL
      cd /builddir
      dotnet fable npm-run integration-test
      cp /builddir/results.xml /vagrant
    SHELL

    test.vm.provision 'sync-tests', type: 'shell', run: 'never', inline: <<-SHELL
      rsync -avzh /vagrant/IML.IntegrationTest/*.fs /builddir/IML.IntegrationTest
    SHELL

    test.vm.provision 'update-snapshot', type: 'shell', run: 'never', inline: <<-SHELL
      cd /builddir
      dotnet fable npm-run integration-test -- -u
      cp -rf IML.IntegrationTest/__snapshots__/* /vagrant/IML.IntegrationTest/__snapshots__
    SHELL
  end
end
