
call RefreshEnv.Cmd

dotnet test tests --configuration Release -v normal %*
%TEMP%\depends\depends.exe /c /ot:%TEMP%\depends.log tests/bin/Release/netcoreapp2.1/aws-crt-dotnet.dll
type %TEMP%\depends.log

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
