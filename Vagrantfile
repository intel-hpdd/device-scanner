# -*- mode: ruby -*-
# vi: set ft=ruby :

require 'open3'

NAME_SUFFIX = (ENV['NAME_SUFFIX'] || '').freeze
INT_NET_NAME = "scanner-net#{NAME_SUFFIX}".freeze

Vagrant.configure('2') do |config|
  config.vm.box = 'centos/7'

  config.vm.provider 'virtualbox' do |v|
    v.linked_clone = true
  end

  create_hostfile(config)
  create_ssh_keys(config)

  ISCSI_NAME = "device-scanner-iscsi#{NAME_SUFFIX}".freeze

  config.vm.define ISCSI_NAME do |iscsi|
    iscsi.vm.host_name = 'iscsi.local'

    iscsi.vm.provider 'virtualbox' do |v|
      v.name = ISCSI_NAME
      v.memory = 256

      create_iscsi_disks(v)
    end

    configure_private_network(
      iscsi,
      ['10.0.40.10', '10.0.50.10'],
      "device-scanner-iscsi-net#{NAME_SUFFIX}"
    )

    create_iscsi_targets(iscsi)
  end

  SCANNER_NAME = 'device-scanner'.freeze

  config.vm.define "#{SCANNER_NAME}#{NAME_SUFFIX}" do |device_scanner|
    device_scanner.vm.host_name = 'device-scanner1.local'

    device_scanner.vm.provider 'virtualbox' do |v|
      v.memory = 256
      v.cpus = 4
      v.name = "#{SCANNER_NAME}#{NAME_SUFFIX}"
    end

    configure_private_network(
      device_scanner,
      ['10.0.10.10'],
      "device-scanner-net#{NAME_SUFFIX}"
    )

    configure_private_network(
      device_scanner,
      ['10.0.40.11', '10.0.50.11'],
      "device-scanner-iscsi-net#{NAME_SUFFIX}"
    )

    provision_iscsi_client(device_scanner)
    provision_mpath(device_scanner)
    create_certs(device_scanner)

    device_scanner.vm.provision 'setup', type: 'shell', inline: <<-SHELL
      yum install -y epel-release http://download.zfsonlinux.org/epel/zfs-release.el7_5.noarch.rpm
      yum install -y htop
      mkdir -p /etc/iml
      echo 'IML_MANAGER_URL=https://adm.local' > /etc/iml/manager-url.conf
    SHELL
  end

  # Create test node
  TEST_NAME = 'test'.freeze
  config.vm.define "#{TEST_NAME}#{NAME_SUFFIX}" do |test|
    test.vm.hostname = "#{TEST_NAME}.local"

    test.vm.provider 'virtualbox' do |v|
      v.memory = 512
      v.cpus = 4
      v.name = "#{TEST_NAME}#{NAME_SUFFIX}"
    end

    configure_private_network(
      test,
      ['10.0.10.30'],
      "device-scanner-net#{NAME_SUFFIX}"
    )

    test.vm.provision 'deps', type: 'shell', inline: <<-SHELL
      yum install -y epel-release
      yum install -y cargo rust pdsh rpm-build upx openssl-devel
    SHELL

    test.vm.provision 'build', type: 'shell', inline: <<-SHELL
      rm -rf /tmp/_topdir
      cd /vagrant/device-types
      cargo package --no-verify --allow-dirty
      cd /vagrant/device-scanner-daemon
      cargo package --no-verify --allow-dirty
      cd /vagrant/uevent-listener
      cargo package --no-verify --allow-dirty
      cd /vagrant/mount-emitter
      cargo package --no-verify --allow-dirty
      cd /vagrant/device-scanner-proxy
      cargo package --no-verify --allow-dirty
      cd /vagrant/futures-failure
      cargo package --no-verify --allow-dirty
      mkdir -p /tmp/_topdir/SOURCES
      mv -f /vagrant/target/package/*.crate /tmp/_topdir/SOURCES/
      cd /vagrant
      rpmbuild -bs --define "_topdir /tmp/_topdir" /vagrant/iml-device-scanner.spec
      rpmbuild --rebuild --define "_topdir /tmp/_topdir" --define="devel_build 1" /tmp/_topdir/SRPMS/iml-device-scanner-2.0.0-1.el7.src.rpm
    SHELL

    test.vm.provision 'deploy', type: 'shell', inline: <<-SHELL
      scp /tmp/_topdir/RPMS/x86_64/iml-device-scanner-*.rpm root@device-scanner1.local:/tmp
      pdsh -w device-scanner[1].local yum install -y /tmp/*.rpm
      pdsh -w device-scanner[1].local systemctl start device-scanner.target
    SHELL
  end
end

# Checks if a scsi controller exists.
# This is used as a predicate to create controllers, as vagrant does not provide this
# functionality by default.
def controller_exists(name, controller_name)
  out, err = Open3.capture2e("VBoxManage showvminfo #{name}")

  return false if err.exitstatus != 0

  out.split(/\n/)
     .select { |x| x.start_with? 'Storage Controller Name' }
     .map { |x| x.split(':')[1].strip }
     .any? { |x| x == controller_name }
end

# Creates a hosts file that is distributed to each node.
def create_hostfile(config)
  open('hosts', 'w') do |f|
    f.puts <<-__EOF
127.0.0.1   localhost localhost.localdomain localhost4 localhost4.localdomain4
::1         localhost localhost.localdomain localhost6 localhost6.localdomain6

10.0.40.10 iscsi.local iscsi
10.0.10.10 device-scanner1.local device-scanner1
10.0.50.10 iscsi2.local iscsi2
    __EOF
  end

  config.vm.provision 'shell', inline: 'cp -f /vagrant/hosts /etc/hosts'
end

# Creates SSH keys that are shared between hosts
def create_ssh_keys(config)
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
end

# Creates a SATA Controller and attaches 10 disks to it
def create_iscsi_disks(vbox)
  unless controller_exists(ISCSI_NAME, 'SATA Controller')
    vbox.customize ['storagectl', :id,
                    '--name', 'SATA Controller',
                    '--add', 'sata']
  end

  (1..10).each do |i|
    id = i.to_s.rjust(2, '0')
    disk = "./tmp/disk#{i}.vdi"

    unless File.exist?(disk)
      vbox.customize ['createmedium', 'disk',
                      '--filename', disk,
                      '--size', '100',
                      '--format', 'VDI',
                      '--variant', 'fixed']
    end

    vbox.customize [
      'storageattach', :id,
      '--storagectl', 'SATA Controller',
      '--port', i,
      '--type', 'hdd',
      '--medium', disk
    ]
    vbox.customize [
      'setextradata', :id,
      "VBoxInternal/Devices/ahci/0/Config/Port#{i}/SerialNumber",
      "081118FC1221NCJ6G8#{id}"
    ]
  end
end

# Utilizes a private network for the given vm and ips
def configure_private_network(config, ips, net_name)
  ips.each do |ip|
    config.vm.network 'private_network',
                      ip: ip,
                      netmask: '255.255.255.0',
                      virtualbox__intnet: net_name
  end
end

# Creates iscsi targets
def create_iscsi_targets(iscsi)
  disk_commands = ('b'..'z')
                  .take(10)
                  .flat_map.with_index do |x, i|
    [
      "targetcli /backstores/block create disk#{i + 1} /dev/sd#{x}",
      "targetcli /iscsi/iqn.2015-01.com.whamcloud.lu:disks/tpg1/luns/ create /backstores/block/disk#{i + 1}"
    ]
  end.join "\n"

  iscsi.vm.provision 'bootstrap', type: 'shell', inline: <<-SHELL
    yum -y install targetcli lsscsi
    targetcli /iscsi set global auto_add_default_portal=false
    targetcli /iscsi create iqn.2015-01.com.whamcloud.lu:disks

    #{disk_commands}
    targetcli /iscsi/iqn.2015-01.com.whamcloud.lu:disks/tpg1/portals/ create 10.0.40.10
    targetcli /iscsi/iqn.2015-01.com.whamcloud.lu:disks/tpg1/portals/ create 10.0.50.10
    targetcli /iscsi/iqn.2015-01.com.whamcloud.lu:disks/tpg1/acls create iqn.2015-01.com.whamcloud:disks
    targetcli saveconfig
    systemctl enable target
  SHELL
end

# Sets up clients to connect to iscsi server
def provision_iscsi_client(config)
  config.vm.provision 'iscsi-client', type: 'shell', inline: <<-SHELL
    yum -y install iscsi-initiator-utils lsscsi
    echo "InitiatorName=iqn.2015-01.com.whamcloud:disks" > /etc/iscsi/initiatorname.iscsi
    iscsiadm --mode discoverydb --type sendtargets --portal 10.0.40.10:3260 --discover
    iscsiadm --mode node --targetname iqn.2015-01.com.whamcloud.lu:disks --portal 10.0.40.10:3260 -o update -n node.startup -v automatic
    iscsiadm --mode node --targetname iqn.2015-01.com.whamcloud.lu:disks --portal 10.0.40.10:3260 -o update -n node.conn[0].startup -v automatic
    iscsiadm --mode node --targetname iqn.2015-01.com.whamcloud.lu:disks --portal 10.0.50.10:3260 -o update -n node.startup -v automatic
    iscsiadm --mode node --targetname iqn.2015-01.com.whamcloud.lu:disks --portal 10.0.50.10:3260 -o update -n node.conn[0].startup -v automatic
    systemctl start iscsi
  SHELL
end

# Sets up multipathing on client
def provision_mpath(config)
  config.vm.provision 'mpath', type: 'shell', inline: <<-SHELL
    yum -y install device-mapper-multipath
    cp /usr/share/doc/device-mapper-multipath-*/multipath.conf /etc/multipath.conf
    systemctl start multipathd.service
    systemctl enable multipathd.service
  SHELL
end

def create_certs(config)
  config.vm.provision 'certs', type: 'shell', inline: <<-SHELL
      mkdir -p /etc/iml
      cd /etc/iml
      openssl req \
        -subj '/CN=managernode.com/O=Whamcloud/C=US' \
        -newkey rsa:1024 -nodes -keyout manager.key \
        -x509 -days 365 -out manager.crt
      openssl dhparam -out manager.pem 1024
      openssl pkcs12 -export -out certificate.pfx -inkey manager.key -in manager.crt -passout pass:
  SHELL
end
