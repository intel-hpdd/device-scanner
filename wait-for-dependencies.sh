#!/bin/bash

set -e

echo "Starting dependency check"

until [ -f /var/lib/chroma/iml-settings.conf ] && psql -h 'postgres' -U 'chroma' -c '\q'; do
  echo "Waiting for dependencies"
  sleep 5
done

echo "Dependency check passed"

set -a
source /var/lib/chroma/iml-settings.conf
set +a

exec $@
