
call RefreshEnv.Cmd

dotnet test tests -v normal %* || goto :error
%TEMP%\depends\depends.exe /c /ot:%TEMP%\depends.log tests/bin/Debug/netcoreapp2.1/aws-crt-dotnet.dll
type %TEMP%\depends.log

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
