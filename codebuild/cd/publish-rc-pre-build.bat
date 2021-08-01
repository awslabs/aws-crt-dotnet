
set PATH=%PATH%;"C:/Program Files/Git/usr/bin"
md packages
md build

xcopy /S /Y %CODEBUILD_SRC_DIR_linux_x64%\dist\* %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build\ || goto :error
xcopy /S /Y %CODEBUILD_SRC_DIR_win_x86%\dist\* %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build\ || goto :error
xcopy /S /Y %CODEBUILD_SRC_DIR_win_x64%\dist\* %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build\ || goto :error
xcopy /S /Y %CODEBUILD_SRC_DIR_osx_x64%\* %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build\ || goto :error

git describe --tags | cut -f1 -d'-' | cut -f2 -dv > version.txt
set /P PKG_VERSION=<version.txt

aws s3 cp --recursive s3://aws-crt-dotnet-pipeline/v${PKG_VERSION}/lib %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build   

dir /S %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%