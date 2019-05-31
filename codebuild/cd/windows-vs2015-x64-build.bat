
@setlocal enableextensions enabledelayedexpansion

dotnet build -c Release -p:CMakeGenerator="Visual Studio 14 2015 Win64" || goto error

md ..\dist\x64
for /R c:\build-aws-crt\lib %%F IN (*) do (
    if %%~xF != ".ilk" (
        copy %%F ..\dist\x64\
    )
)

@endlocal
goto :EOF

:error
@endlocal
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
