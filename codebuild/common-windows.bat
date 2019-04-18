
call RefreshEnv.Cmd

dotnet test tests -v normal --configuration=RelWithDebInfo %* || goto :error

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
