
@setlocal enableextensions enabledelayedexpansion

dotnet build -f netstandard2.0 --configuration Release -p:PlatformTarget=x86 -p:CMakeGenerator="Visual Studio 14 2015" || goto error

md ..\dist\x86
for /R c:\build-aws-crt\lib %%F IN (*) do (
    if NOT "%%~xF" == ".ilk" (
        copy %%F ..\dist\
    )
)

@endlocal
goto :EOF

:error
@endlocal
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
