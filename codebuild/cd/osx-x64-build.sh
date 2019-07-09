#!/usr/bin/env bash

set -ex

dotnet build --configuration Release
mkdir -p ../dist/x64
cp -rv build/lib/libaws-crt-dotnet-x64.dylib ../dist/
