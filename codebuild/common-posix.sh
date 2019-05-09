#!/bin/bash

set -ex

# grab external dependencies, ignoring that they don't exist in the packages folder
dotnet restore --ignore-failed-sources
# test will build and package, then run tests
dotnet test -v normal

if [ "$AWS_DOTNET_RUNTIME" == "" ]; then
    exit 0
fi

dotnet publish --self-contained --runtime $AWS_DOTNET_RUNTIME tools/Elasticurl

python3 build/deps/build/src/AwsCHttp/integration-testing/http_client_test.py tools/Elasticurl/bin/Debug/netcoreapp2.2/$AWS_DOTNET_RUNTIME/publish/Elasticurl.NET
