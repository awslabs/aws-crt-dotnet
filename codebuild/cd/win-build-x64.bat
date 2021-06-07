
@setlocal enableextensions enabledelayedexpansion

dotnet build -f netstandard2.0 --configuration Release -p:PlatformTarget=x64 -p:CMakeGenerator64="Visual Studio 14 2015 Win64" -p:CMakeGenerator86="Visual Studio 14 2015" || goto error

md ..\dist\x64
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
