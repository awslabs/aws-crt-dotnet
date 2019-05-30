
@setlocal enableextensions enabledelayedexpansion

dotnet build -c Release -p:CMakeGenerator="Visual Studio 14 2015 Win64" || goto error

robocopy c:\build-aws-crt\build\lib ..\dist\x86 *.* /xf *.ilk || goto error

@endlocal
goto :EOF

:error
@endlocal
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
