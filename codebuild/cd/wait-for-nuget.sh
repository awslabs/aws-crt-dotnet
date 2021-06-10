#!/usr/bin/env bash

set -ex

timeout=1800 # 30 minute timeout
interval=15 # 15 second wait between tries
version=$1

start=$(date +%s)

while true; do
    now=$(date +%s)
    delta=$(expr $now - $start || true)

    awscrt_status=$(curl -IsL https://api.nuget.org/v3/registration5-semver1/awscrt/${version}.json | grep -c ^Error) 
    awscrthttp_status=$(curl -IsL https://api.nuget.org/v3/registration5-semver1/awscrt-http/${version}.json | grep -c ^Error)
    awscrtauth_status=$(curl -IsL https://api.nuget.org/v3/registration5-semver1/awscrt-auth/${version}.json | grep -c ^Error)
    awscrtcal_status=$(curl -IsL https://api.nuget.org/v3/registration5-semver1/awscrt-cal/${version}.json | grep -c ^Error)
    awscrtchecksums_status=$(curl -IsL https://api.nuget.org/v3/registration5-semver1/awscrt-checksums/${version}.json | grep -c ^Error)
    echo "t=${delta}s AWSCRT=${awscrt_status} AWSCRT-HTTP=${awscrthttp_status} AWSCRT-AUTH=${awscrtauth_status} AWSCRT-CAL=${awscrtcal_status} AWSCRT-CHECKSUMS=${awscrtchecksums_status}"
    if [ $awscrt_status -eq 0 ] && [ $awscrthttp_status -eq 0 ] && [ $awscrtauth_status -eq 0 ] && [ $awscrtcal_status -eq 0 ] && [ $awscrtchecksums_status -eq 0 ]; then
        echo "Package(s) with version $version found in NuGet."
        break
    fi
    
    if [ $delta -gt $timeout ]; then
        echo "Timed out waiting for packages to be available, giving up."
        exit 1
    fi

    sleep $interval
done

exit 0