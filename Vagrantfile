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

  config.vm.synced_folder '.', '/vagrant',
                          type: 'rsync',
                          rsync__exclude: ['.git/', 'target/']

  create_hostfile(config)
  create_ssh_keys(config)

  ISCSI_NAME = "device-scanner-iscsi#{NAME_SUFFIX}".freeze

  # Create iscsi server
  config.vm.define ISCSI_NAME do |iscsi|
    iscsi.vm.host_name = 'iscsi.local'
    iscsi.vm.provider 'virtualbox' do |v|
      v.name = ISCSI_NAME
      v.cpus = 2
      v.memory = 256
      v.customize ['modifyvm', :id, '--audio', 'none']

      create_iscsi_disks(v)
    end

    configure_private_network(
      iscsi,
      ['10.0.40.10', '10.0.50.10'],
      "device-scanner-iscsi-net#{NAME_SUFFIX}"
    )

    create_iscsi_targets(iscsi)
  end

  SCANNER_NAME = "device-scanner1#{NAME_SUFFIX}".freeze
  # Create device-scanner node 1
  config.vm.define SCANNER_NAME do |device_scanner1|
    device_scanner1.vm.host_name = 'device-scanner1.local'

    device_scanner1.vm.provider 'virtualbox' do |v|
      v.name = SCANNER_NAME
      v.cpus = 4
      v.memory = 512 # Little more memory to install ZFS
      v.customize ['modifyvm', :id, '--audio', 'none']
    end

    configure_private_network(
      device_scanner1,
      ['10.0.10.10'],
      "device-scanner-net#{NAME_SUFFIX}"
    )

    configure_private_network(
      device_scanner1,
      ['10.0.30.11'],
      "device-aggregator-net#{NAME_SUFFIX}"
    )

    configure_private_network(
      device_scanner1,
      ['10.0.40.11', '10.0.50.11'],
      "device-scanner-iscsi-net#{NAME_SUFFIX}"
    )

    provision_iscsi_client(device_scanner1)
    provision_mpath(device_scanner1)
    install_zfs(device_scanner1)

    device_scanner1.vm.provision 'setup', type: 'shell', inline: <<-SHELL
      yum install -y epel-release
      yum install -y htop jq
      mkdir -p /etc/iml
      echo 'IML_MANAGER_URL=https://device-aggregator.local' > /etc/iml/manager-url.conf
      modprobe zfs
      genhostid
      zpool create test mirror mpatha mpathb cache mpathc spare mpathd mpathe
    SHELL
  end

  SCANNER_NAME2 = "device-scanner2#{NAME_SUFFIX}".freeze
  # Create device-scanner node 2
  config.vm.define SCANNER_NAME2 do |device_scanner2|
    device_scanner2.vm.host_name = 'device-scanner2.local'

    device_scanner2.vm.provider 'virtualbox' do |v|
      v.name = SCANNER_NAME2
      v.cpus = 4
      v.memory = 512 # Little more memory to install ZFS
      v.customize ['modifyvm', :id, '--audio', 'none']
    end

    configure_private_network(
      device_scanner2,
      ['10.0.10.11'],
      "device-scanner-net#{NAME_SUFFIX}"
    )

    configure_private_network(
      device_scanner2,
      ['10.0.30.12'],
      "device-aggregator-net#{NAME_SUFFIX}"
    )

    configure_private_network(
      device_scanner2,
      ['10.0.40.12', '10.0.50.12'],
      "device-scanner-iscsi-net#{NAME_SUFFIX}"
    )

    provision_iscsi_client(device_scanner2)
    provision_mpath(device_scanner2)
    install_zfs(device_scanner2)

    device_scanner2.vm.provision 'setup', type: 'shell', inline: <<-SHELL
      yum install -y epel-release
      yum install -y htop jq
      mkdir -p /etc/iml
      echo 'IML_MANAGER_URL=https://device-aggregator.local' > /etc/iml/manager-url.conf
      genhostid
    SHELL
  end

  # Create aggregator node
  AGGREGATOR_NAME = 'device-aggregator'.freeze
  config.vm.define "#{AGGREGATOR_NAME}#{NAME_SUFFIX}" do |aggregator|
    aggregator.vm.hostname = "#{AGGREGATOR_NAME}.local"

    aggregator.vm.provider 'virtualbox' do |v|
      v.name = "#{AGGREGATOR_NAME}#{NAME_SUFFIX}"
      v.memory = 256
      v.cpus = 4
      v.customize ['modifyvm', :id, '--audio', 'none']
    end

    configure_private_network(
      aggregator,
      ['10.0.30.10'],
      "device-aggregator-net#{NAME_SUFFIX}"
    )

    aggregator.vm.provision 'deps', type: 'shell', inline: <<-SHELL
      yum install -y epel-release
      yum install -y nginx htop jq
    SHELL

    write_nginx_conf(aggregator)
  end

  # Create test node
  TEST_NAME = 'test'.freeze
  config.vm.define "#{TEST_NAME}#{NAME_SUFFIX}" do |test|
    test.vm.hostname = "#{TEST_NAME}.local"

    test.vm.provider 'virtualbox' do |v|
      v.name = "#{TEST_NAME}#{NAME_SUFFIX}"
      v.cpus = 8
      v.memory = 1024 # little more memory for building
      v.customize ['modifyvm', :id, '--audio', 'none']
    end

    configure_private_network(
      test,
      ['10.0.10.12'],
      "device-scanner-net#{NAME_SUFFIX}"
    )

    configure_private_network(
      test,
      ['10.0.30.11'],
      "device-aggregator-net#{NAME_SUFFIX}"
    )

    install_zfs(config)

    test.vm.provision 'deps', type: 'shell', inline: <<-SHELL
      yum install -y epel-release
      # Install epel-testing so we can get rust >= 1.30
      yum-config-manager --enable epel-testing
      yum install -y pdsh rpm-build openssl-devel tree gcc
      yum -y install yum-plugin-copr
      yum -y copr enable alonid/llvm-5.0.0
      yum -y install clang-5.0.0 libzfs2-devel --nogpgcheck
      yum -y install cargo
    SHELL

    test.vm.provision 'build', type: 'shell', inline: <<-SHELL
      rm -rf /tmp/_topdir
      cd /vagrant/device-types
      cargo package --no-verify --allow-dirty
      cd /vagrant/device-scanner-daemon
      cargo package --no-verify --allow-dirty
      cd /vagrant/device-aggregator
      cargo package --no-verify --allow-dirty
      cd /vagrant/uevent-listener
      cargo package --no-verify --allow-dirty
      cd /vagrant/mount-emitter
      cargo package --no-verify --allow-dirty
      cd /vagrant/device-scanner-zedlets
      cargo package --no-verify --allow-dirty
      cd /vagrant/device-scanner-proxy
      cargo package --no-verify --allow-dirty
      cd /vagrant/futures-failure
      cargo package --no-verify --allow-dirty
      cd /vagrant/zed-enhancer
      cargo package --no-verify --allow-dirty
      mkdir -p /tmp/_topdir/SOURCES
      mv -f /vagrant/target/package/*.crate /tmp/_topdir/SOURCES/
      cd /vagrant
      rpmbuild -bs --define "_topdir /tmp/_topdir" /vagrant/iml-device-scanner.spec
      rpmbuild --rebuild --define "_topdir /tmp/_topdir" --define="devel_build 1" /tmp/_topdir/SRPMS/iml-device-scanner-2.0.0-1.el7.src.rpm
    SHELL

    distribute_certs(test)

    test.vm.provision 'deploy', type: 'shell', inline: <<-SHELL
      pdsh -w device-scanner[1].local,device-aggregator.local 'yum remove -y iml-device-scanner-*' | dshbak

      scp /tmp/_topdir/RPMS/x86_64/iml-device-scanner-aggregator-*.rpm root@device-aggregator.local:/tmp
      pdsh -w device-aggregator.local yum install -y /tmp/iml-device-scanner-aggregator-*.rpm
      ssh root@device-aggregator.local systemctl enable --now device-aggregator.service nginx

      scp /tmp/_topdir/RPMS/x86_64/iml-device-scanner-[0-9]*.rpm root@device-scanner1.local:/tmp
      scp /tmp/_topdir/RPMS/x86_64/iml-device-scanner-proxy-[0-9]*.rpm root@device-scanner1.local:/tmp
      scp /tmp/_topdir/RPMS/x86_64/iml-device-scanner-[0-9]*.rpm root@device-scanner2.local:/tmp
      scp /tmp/_topdir/RPMS/x86_64/iml-device-scanner-proxy-[0-9]*.rpm root@device-scanner2.local:/tmp
      pdsh -w device-scanner[1-2].local 'yum install -y /tmp/*.rpm' | dshbak
      pdsh -w device-scanner[1-2].local systemctl enable --now device-scanner.target | dshbak
    SHELL
  end
end

# Checks if a scsi controller exists.
# This is used as a predicate to create controllers,
# as vagrant does not provide this
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

10.0.10.10 device-scanner1.local device-scanner1
10.0.10.11 device-scanner2.local device-scanner2
10.0.30.10 device-aggregator.local device-aggregator
10.0.40.10 iscsi.local iscsi
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

# Installs zfs
def install_zfs(config)
  config.vm.provision 'zfs', type: 'shell', inline: <<-SHELL
    yum install -y http://download.zfsonlinux.org/epel/zfs-release.el7_5.noarch.rpm
    yum-config-manager --disable zfs
    yum-config-manager --enable zfs-kmod
    yum install -y zfs
  SHELL
end

# Create certs and distribute them
# to device-scanner-proxy and nginx proxy
def distribute_certs(config)
  config.vm.provision 'distribute_certs', type: 'shell', inline: <<-SHELL
    mkdir -p /tmp/certs
    # Authority key and cert
    openssl genrsa -out /tmp/certs/authority.pem 2048 -sha256
    openssl req -new -sha256 -subj /C=AA/ST=AA/L=Location/O=Org/CN=x_local_authority -key /tmp/certs/authority.pem | openssl x509 -req -sha256 -signkey /tmp/certs/authority.pem -out /tmp/certs/authority.crt

    # Manager key and cert
    openssl genrsa -out /tmp/certs/manager.pem 2048 -sha256
    openssl req -new -sha256 -key /tmp/certs/manager.pem -subj /C=/ST=/L=/O=/CN=device-aggregator.local | openssl x509 -req -sha256 -CAkey /tmp/certs/authority.pem -CA /tmp/certs/authority.crt -CAcreateserial -out /tmp/certs/manager.crt

    # Send certs to aggregator
    pdsh -w device-aggregator.local rm -rf /var/lib/chroma
    pdsh -w device-aggregator.local mkdir -p /var/lib/chroma
    scp /tmp/certs/* root@device-aggregator.local:/var/lib/chroma

    pdsh -w device-scanner[1,2].local rm -rf /etc/iml/certificate.pfx

    # Device-scanner client-cert pfx
    openssl genrsa -out /tmp/certs/private.pem 2048
    openssl req -new -subj /C=/ST=/L=/O=/CN=device-scanner1.local -key /tmp/certs/private.pem | openssl x509 -req -CAkey /tmp/certs/authority.pem -CA /tmp/certs/authority.crt -CAcreateserial -sha256 -out /tmp/certs/self.crt
    openssl pkcs12 -export -out /tmp/certs/certificate.pfx -inkey /tmp/certs/private.pem -in /tmp/certs/self.crt -passout pass:

    scp /tmp/certs/certificate.pfx root@device-scanner1.local:/etc/iml

    rm -rf /tmp/certs/{private.pem, self.crt, certificate.pfx}

    # Device-scanner client-cert pfx
    openssl genrsa -out /tmp/certs/private.pem 2048
    openssl req -new -subj /C=/ST=/L=/O=/CN=device-scanner2.local -key /tmp/certs/private.pem | openssl x509 -req -CAkey /tmp/certs/authority.pem -CA /tmp/certs/authority.crt -CAcreateserial -sha256 -out /tmp/certs/self.crt
    openssl pkcs12 -export -out /tmp/certs/certificate.pfx -inkey /tmp/certs/private.pem -in /tmp/certs/self.crt -passout pass:

    scp /tmp/certs/certificate.pfx root@device-scanner2.local:/etc/iml
  SHELL
end

NGINX_CONF = <<-__EOF
  map $ssl_client_s_dn $ssl_client_s_dn_cn {
    default "";
    ~CN=(?<CN>[^,]+) $CN;
  }

  error_log syslog:server=unix:/dev/log;
  access_log syslog:server=unix:/dev/log;

  server {
      listen 80;

      location /device-aggregator {
        proxy_pass http://127.0.0.1:8008;
      }
  }

  server {
      listen 443 ssl http2;

      error_page 497 https://$http_host$request_uri;

      ssl_certificate /var/lib/chroma/manager.crt;
      ssl_certificate_key /var/lib/chroma/manager.pem;
      ssl_trusted_certificate /var/lib/chroma/authority.crt;
      ssl_client_certificate /var/lib/chroma/authority.crt;
      ssl_verify_client on;

      ssl_protocols TLSv1.2;
      ssl_prefer_server_ciphers on;
      ssl_ciphers 'ECDHE-RSA-AES128-GCM-SHA256:!DH+3DES:!ADH:!AECDH:!RC4:!aNULL:!MD5';

      ssl_session_cache shared:SSL:10m;


      location /iml-device-aggregator {
        if ($ssl_client_verify != SUCCESS) {
          return 401;
        }

        proxy_set_header X-SSL-Client-On $ssl_client_verify;
        proxy_set_header X-SSL-Client-Name $ssl_client_s_dn_cn;
        proxy_set_header X-SSL-Client-Serial $ssl_client_serial;

        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Server $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header Http-Host $http_host;

        proxy_pass http://127.0.0.1:8008;
      }
  }
__EOF
             .freeze

# Write out the proxy.conf needed
# by device-aggregator for SSL termination
# from device-scanner-proxy services.
def write_nginx_conf(config)
  config.vm.provision 'nginx',
                      type: 'shell',
                      inline: <<-SHELL
                        echo '#{NGINX_CONF}' > /etc/nginx/conf.d/proxy.conf
                      SHELL
end
