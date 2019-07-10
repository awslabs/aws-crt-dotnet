#!/usr/bin/env bash

set -ex

if ! type -P dotnet &> /dev/null; then
    if [ ! -e ~/.dotnet ]; then
        cd /tmp
        curl -LO https://dot.net/v1/dotnet-install.sh
        chmod u+x dotnet-install.sh
        ./dotnet-install.sh --channel 2.2
    fi
    export PATH=$PATH:~/.dotnet
fi

dotnet build --configuration Release
mkdir -p ../dist/x64
cp -rv build/lib/libaws-crt-dotnet-x64.dylib ../dist/
