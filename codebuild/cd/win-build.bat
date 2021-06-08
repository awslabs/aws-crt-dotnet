
@setlocal enableextensions enabledelayedexpansion

call "C:/Program Files (x86)/Microsoft Visual Studio/2019/BuildTools/Common7/Tools/VsDevCmd.bat"

dotnet build -f netstandard2.0 --configuration Release -p:PlatformTarget=%1 || goto error

md ..\dist\%1
for /R .\build\%1\lib %%F IN (*) do (
    if NOT "%%~xF" == ".ilk" (
        copy %%F ..\dist\%1\
    )
)

@endlocal
goto :EOF

:error
@endlocal
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
