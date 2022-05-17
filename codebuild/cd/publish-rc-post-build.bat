

set PATH=%PATH%;"C:/Program Files/Git/usr/bin"

git describe --tags | cut -f1 -d'-' | cut -f2 -dv > version.txt
set /P PKG_VERSION=<version.txt

bash %CODEBUILD_SRC_DIR%/aws-crt-dotnet/codebuild/cd/wait-for-nuget.sh %PKG_VERSION%-rc1 || goto error

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
