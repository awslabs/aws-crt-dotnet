
@setlocal enableextensions enabledelayedexpansion

dotnet build -c Release -p:CMakeGenerator="Visual Studio 14 2015" || goto error

for /f %%A in ('git describe --tags') do (
    set GIT_TAG=%%A
)

for /f "tokens=1 delims=-" %%A in ("!GIT_TAG!") do (
    set GIT_TAG=%%A
)

for /f "tokens=1 delims=v" %%A in ("!GIT_TAG!") do (
    set GIT_TAG=%%A
)

robocopy .\build\lib ..\dist\x86 *.dll || goto error

@endlocal
goto :EOF

:error
@endlocal
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
