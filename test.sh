vagrant destroy -f
export NAME_SUFFIX=$(pwd | sha256sum | head -c 32)
vagrant up
vagrant destroy -f
