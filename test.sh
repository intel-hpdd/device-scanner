export NAME_SUFFIX=$(pwd | sha256sum | head -c 32)
vagrant destroy -f 
vagrant up
