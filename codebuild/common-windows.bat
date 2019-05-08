
call RefreshEnv.Cmd

dotnet pack aws-crt --configuration Release --output %CD%\packages
dotnet pack aws-crt-http --configuration Release --output %CD%\packages
dotnet test --configuration Release -v normal %*
:: %TEMP%\depends\depends.exe /c /ot:%TEMP%\depends.log tests/bin/Release/netcoreapp2.1/aws-crt-dotnet.dll
:: type %TEMP%\depends.log

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
