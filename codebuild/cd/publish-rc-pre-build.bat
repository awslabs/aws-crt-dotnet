
set PATH=%PATH%;"C:/Program Files/Git/usr/bin"
md packages
md build

xcopy /S /Y %CODEBUILD_SRC_DIR_linux_x64%\dist\* %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build\ || goto :error
xcopy /S /Y %CODEBUILD_SRC_DIR_win_x86%\dist\* %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build\ || goto :error
xcopy /S /Y %CODEBUILD_SRC_DIR_win_x64%\dist\* %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build\ || goto :error
xcopy /S /Y %CODEBUILD_SRC_DIR_osx_x64%\dist\* %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build\ || goto :error
dir /S %CODEBUILD_SRC_DIR%\aws-crt-dotnet\build

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%