#!/bin/bash

set -ex

dotnet pack aws-crt --output `pwd`/packages
dotnet pack aws-crt-http --output `pwd`/packages
dotnet test -v normal

if [ "$AWS_DOTNET_RUNTIME" == "" ]; then
    exit 0
fi

dotnet publish --self-contained --runtime $AWS_DOTNET_RUNTIME tools/Elasticurl

python3 build/deps/build/src/AwsCHttp/integration-testing/http_client_test.py tools/Elasticurl/bin/Debug/netcoreapp2.2/$AWS_DOTNET_RUNTIME/publish/Elasticurl.NET
