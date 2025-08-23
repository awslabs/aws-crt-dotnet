@echo off
REM Windows batch file equivalent of clean_rebuild.sh

REM Update git submodules
git submodule update --init

REM Remove build directories and artifacts
if exist build rmdir /s /q build
if exist packages rmdir /s /q packages
if exist aws-crt\bin rmdir /s /q aws-crt\bin
if exist aws-crt\obj rmdir /s /q aws-crt\obj
if exist aws-crt-http\bin rmdir /s /q aws-crt-http\bin
if exist aws-crt-http\obj rmdir /s /q aws-crt-http\obj
if exist aws-crt-auth\bin rmdir /s /q aws-crt-auth\bin
if exist aws-crt-auth\obj rmdir /s /q aws-crt-auth\obj
if exist aws-crt-checksums\bin rmdir /s /q aws-crt-checksums\bin
if exist aws-crt-checksums\obj rmdir /s /q aws-crt-checksums\obj
if exist aws-crt-cal\bin rmdir /s /q aws-crt-cal\bin
if exist aws-crt-cal\obj rmdir /s /q aws-crt-cal\obj
if exist tests\bin rmdir /s /q tests\bin
if exist tests\obj rmdir /s /q tests\obj
if exist tools\Elasticurl\bin rmdir /s /q tools\Elasticurl\bin
if exist tools\Elasticurl\obj rmdir /s /q tools\Elasticurl\obj

REM Remove NuGet packages (using PowerShell for wildcard support)
powershell -Command "if (Test-Path $env:USERPROFILE\.nuget\packages\awscrt*) { Remove-Item -Path $env:USERPROFILE\.nuget\packages\awscrt* -Force -Recurse }"

REM Build and pack the project
dotnet build -f netstandard2.0 -p:PlatformTarget=x64 -p:CMakeConfig=Debug --configuration Debug
dotnet pack -p:TargetFrameworks=netstandard2.0 --configuration Debug
