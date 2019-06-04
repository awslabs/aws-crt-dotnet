#!/bin/bash

set -ex

# packages dir needs to exist
mkdir -p packages
# test will build and package, then run tests
dotnet test -v normal

if [ "$AWS_DOTNET_RUNTIME" == "" ]; then
    exit 0
fi

dotnet publish --self-contained --runtime $AWS_DOTNET_RUNTIME tools/Elasticurl

curl -L -o /tmp/http_client_test.py https://raw.githubusercontent.com/awslabs/aws-c-http/master/integration-testing/http_client_test.py
python3 /tmp/http_client_test.py tools/Elasticurl/bin/Debug/netcoreapp2.1/$AWS_DOTNET_RUNTIME/publish/Elasticurl.NET
