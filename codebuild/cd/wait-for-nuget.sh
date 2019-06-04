#!/usr/bin/env bash

set -ex

timeout=900 # 15 minute timeout
version=$1

start=$(date +%s)

while true; do
    now=$(date +%s)
    delta=$(expr $now - $start || true)

    awscrt_status=$(curl -IsL https://api.nuget.org/v3/registration3/awscrt/${version}.json | grep -e ^HTTP | tail -1 | grep -c 200 || true) 
    awscrthttp_status=$(curl -IsL https://api.nuget.org/v3/registration3/awscrt-http/${version}.json | grep -e ^HTTP | tail -1 | grep -c 200 || true)
    echo "t=${delta}s AWSCRT=${awscrt_status} AWSCRT-HTTP=${awscrthttp_status}"
    if [ $awscrt_status -gt 0 ] && [ $awscrthttp_status -gt 0 ]; then
        echo "Package(s) with version $version found in NuGet."
        break
    fi
    
    if [ $delta -gt $timeout ]; then
        echo "Timed out waiting for packages to be available, giving up."
        exit 1
    fi

    sleep 15
done

exit 0