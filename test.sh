vagrant destroy -f
export NAME_SUFFIX=$(pwd | sha256sum | head -c 32)
vagrant up device-scanner$NAME_SUFFIX test$NAME_SUFFIX
vagrant destroy -f
