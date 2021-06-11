
@setlocal enableextensions enabledelayedexpansion

call "C:/Program Files (x86)/Microsoft Visual Studio/2019/BuildTools/Common7/Tools/VsDevCmd.bat"

set PATH=%PATH%;"C:/Program Files/Git/usr/bin"

bash .\codebuild\cd\pull-signing-secrets.sh

dotnet build -f netstandard2.0 --configuration Release -p:PlatformTarget=x64 || goto error

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
