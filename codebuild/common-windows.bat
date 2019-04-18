
call RefreshEnv.Cmd

dotnet build tests --configuration=RelWithDebInfo %* || goto :error
%TEMP%\depends\depends.exe /c /ot:%TEMP%\depends.log tests/bin/Debug/netcoreapp2.1/aws-crt-dotnet.dll
type %TEMP%\depends.log

dotnet test tests -v normal --configuration=RelWithDebInfo %* || goto :error

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
