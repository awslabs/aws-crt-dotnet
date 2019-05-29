
@setlocal enableextensions enabledelayedexpansion

dotnet build -c Release -p:CMakeGenerator="Visual Studio 14 2015" || goto error

for /f %%A in ('git describe --tags') do (
    set GIT_TAG=%%A
)

aws s3 cp --recursive --exclude "*" --include "*.dll" .\build\lib s3://aws-crt-java-pipeline/%GIT_TAG%/x86

@endlocal
goto :EOF

:error
@endlocal
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
