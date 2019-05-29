
call RefreshEnv.Cmd

:: grab external dependencies, ignoring that they don't exist in the packages folder
dotnet restore --ignore-failed-sources
:: test will build and package, then run tests
dotnet test --configuration Release -v normal %* || goto :error
:: %TEMP%\depends\depends.exe /c /ot:%TEMP%\depends.log tests/bin/Release/netcoreapp2.1/aws-crt-dotnet.dll
:: type %TEMP%\depends.log

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
