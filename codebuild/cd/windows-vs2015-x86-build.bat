
@setlocal enableextensions enabledelayedexpansion

dotnet build -c Release -p:CMakeGenerator="Visual Studio 14 2015" || goto error

dir /s build
robocopy .\build\lib ..\dist\x86 *.* /xf *.ilk || goto error

@endlocal
goto :EOF

:error
@endlocal
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
