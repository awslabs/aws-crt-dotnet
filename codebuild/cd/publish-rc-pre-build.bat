
set PATH=%PATH%;"C:/Program Files/Git/usr/bin"
md packages
md build

xcopy /S /Y %CODEBUILD_SRC_DIR_combined_builds_a%\* %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build\ || goto :error
xcopy /S /Y %CODEBUILD_SRC_DIR_combined_builds_b%\* %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build\ || goto :error

git describe --tags | cut -f1 -d'-' | cut -f2 -dv > version.txt
set /P PKG_VERSION=<version.txt
echo %PKG_VERSION%

dir /S %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
