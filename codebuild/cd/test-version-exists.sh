#!/usr/bin/env bash
set -ex
# force a failure if there's no tag
git describe --tags
# now get the tag
CURRENT_TAG=$(git describe --tags | cut -f2 -dv)
# convert v0.2.12-2-g50254a9 to 0.2.12
CURRENT_TAG_VERSION=$(git describe --tags | cut -f1 -d'-' | cut -f2 -dv)
# if there's a hash on the tag, then this is not a release tagged commit
if [ "$CURRENT_TAG" != "$CURRENT_TAG_VERSION" ]; then
    echo "Current tag version is not a release tag, cut a new release if you want to publish."
    exit 1
fi
AWSCRT_STATUS=$(curl -IsL https://api.nuget.org/v3/registration3/awscrt/${CURRENT_TAG_VERSION}.json | grep -e ^HTTP | tail -1 | grep -c 200 || true) 
AWSCRTHTTP_STATUS=$(curl -IsL https://api.nuget.org/v3/registration3/awscrt-http/${CURRENT_TAG_VERSION}.json | grep -e ^HTTP | tail -1 | grep -c 200 || true)
if [ $AWSCRT_STATUS -gt 0 ] && [ $AWSCRTHTTP_STATUS -gt 0 ]; then
    echo "$CURRENT_TAG_VERSION is already in NuGet, cut a new release if you want to publish."
    exit 1
fi
# If a pipeline fails, it's possible to strand a RC, but a new version will allow the pipeline to continue
AWSCRT_STATUS=$(curl -IsL https://api.nuget.org/v3/registration3/awscrt/${CURRENT_TAG_VERSION}-rc.json | grep -e ^HTTP | tail -1 | grep -c 200 || true) 
AWSCRTHTTP_STATUS=$(curl -IsL https://api.nuget.org/v3/registration3/awscrt-http/${CURRENT_TAG_VERSION}-rc.json | grep -e ^HTTP | tail -1 | grep -c 200 || true)
if [ $AWSCRT_STATUS -gt 0 ] && [ $AWSCRTHTTP_STATUS -gt 0 ]; then
    echo "$CURRENT_TAG_VERSION is already in NuGet as a release candidate, cut a new release if you want to publish."
    exit 1
fi

echo "$CURRENT_TAG_VERSION currently does not exist in NuGet, allowing pipeline to continue."
exit 0
