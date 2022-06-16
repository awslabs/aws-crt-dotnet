#!/usr/bin/env bash

set -ex

cd /tmp
curl -LO https://dot.net/v1/dotnet-install.sh
chmod u+x dotnet-install.sh
./dotnet-install.sh --version latest
cd -

if ! type -P dotnet &> /dev/null; then
    export PATH=$PATH:~/.dotnet
fi

mkdir packages
git submodule update --init
dotnet build -f netstandard2.0 --configuration Release -p:AwsCrtPlatformTarget=x64
mkdir -p ../dist/x64/lib
cp -rv build/x64/lib/libaws-crt-dotnet-x64.dylib ../dist/x64/lib
