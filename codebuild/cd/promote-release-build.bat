
set PATH=%PATH%;"C:/Program Files/Git/usr/bin"

git describe --tags | cut -f1 -d'-' | cut -f2 -dv > version.txt
set /P PKG_VERSION=<version.txt
echo %PKG_VERSION%

call "C:/Program Files (x86)/Microsoft Visual Studio/2019/BuildTools/Common7/Tools/VsDevCmd.bat"

bash .\codebuild\cd\pull-signing-secrets.sh

dotnet build --configuration Release -p:Version=%PKG_VERSION% -p:PackageVersion=%PKG_VERSION% -p:BuildNativeLibrary=false || goto :error

aws secretsmanager get-secret-value --secret-id "NuGet/push" --query SecretString | cut -f2 -d\ > nuget_key.txt || goto :error
set /P NUGET_KEY=<nuget_key.txt
   
cd packages      
dotnet nuget push AWSCRT.%PKG_VERSION%.nupkg -k %NUGET_KEY% -s https://api.nuget.org/v3/index.json --skip-duplicate || goto :error
dotnet nuget push AWSCRT-HTTP.%PKG_VERSION%.nupkg -k %NUGET_KEY% -s https://api.nuget.org/v3/index.json --skip-duplicate || goto :error
dotnet nuget push AWSCRT-AUTH.%PKG_VERSION%.nupkg -k %NUGET_KEY% -s https://api.nuget.org/v3/index.json --skip-duplicate || goto :error
dotnet nuget push AWSCRT-CAL.%PKG_VERSION%.nupkg -k %NUGET_KEY% -s https://api.nuget.org/v3/index.json --skip-duplicate || goto :error
dotnet nuget push AWSCRT-CHECKSUMS.%PKG_VERSION%.nupkg -k %NUGET_KEY% -s https://api.nuget.org/v3/index.json --skip-duplicate || goto :error

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
