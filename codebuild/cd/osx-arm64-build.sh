#!/usr/bin/env bash

set -ex

cd /tmp
curl -LO https://dot.net/v1/dotnet-install.sh
chmod u+x dotnet-install.sh
./dotnet-install.sh --version latest

if ! type -P dotnet &> /dev/null; then
    export PATH=$PATH:~/.dotnet
fi

cd -

mkdir packages
dotnet build -f netstandard2.0 --configuration Release -p:AwsCrtPlatformTarget=Arm64
mkdir -p ../dist/Arm64/lib
cp -rv build/Arm64/lib/libaws-crt-dotnet-ARM64.dylib ../dist/Arm64/lib
