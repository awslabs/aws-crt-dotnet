#!/bin/bash

set -ex

dotnet test -v normal

dotnet run --project tools/Elasticurl -- -v ERROR -P -H "content-type: application/json" -i -d '{"test": "testval"}' http://httpbin.org/post
dotnet run --project tools/Elasticurl -- -v ERROR -i https://example.com

