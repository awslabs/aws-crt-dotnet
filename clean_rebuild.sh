#!/bin/bash
git submodule update --init

rm -rf build
rm -rf packages
rm -rf aws-crt/bin
rm -rf aws-crt/obj
rm -rf aws-crt-http/bin
rm -rf aws-crt-http/obj
rm -rf aws-crt-auth/bin
rm -rf aws-crt-auth/obj
rm -rf aws-crt-checksums/bin
rm -rf aws-crt-checksums/obj
rm -rf aws-crt-cal/bin
rm -rf aws-crt-cal/obj
rm -rf ~/.nuget/packages/awscrt*
rm -rf tests/bin
rm -rf tests/obj
rm -rf tools/Elasticurl/bin
rm -rf tools/Elasticurl/ob

dotnet build -f netstandard2.0 -p:PlatformTarget=x64 -p:CMakeConfig=Debug
dotnet pack -p:TargetFrameworks=netstandard2.0
