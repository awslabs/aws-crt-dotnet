
set PATH=%PATH%;"C:/Program Files/Git/usr/bin"
md packages
md "build/lib"
cp -rv %CODEBUILD_SRC_DIR_linux_x64%/dist/* %CODEBUILD_SRC_DIR%/aws-crt-dotnet/build/ || goto :error
cp -rv %CODEBUILD_SRC_DIR_win_x86%/dist/* %CODEBUILD_SRC_DIR%/aws-crt-dotnet/build/ || goto :error
cp -rv %CODEBUILD_SRC_DIR_win_x64%/dist/* %CODEBUILD_SRC_DIR%/aws-crt-dotnet/build/ || goto :error
cp -rv %CODEBUILD_SRC_DIR_osx_x64%/dist/* %CODEBUILD_SRC_DIR%/aws-crt-dotnet/build/ || goto :error
ls -alR %CODEBUILD_SRC_DIR%/aws-crt-dotnet/build/lib

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%