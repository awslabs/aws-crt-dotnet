
:: download and install dotnet SDK 2.2
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "Invoke-WebRequest https://download.visualstudio.microsoft.com/download/pr/3c43f486-2799-4454-851c-fa7a9fb73633/673099a9fe6f1cac62dd68da37ddbc1a/dotnet-sdk-2.2.203-win-x64.exe -OutFile %TEMP%\dotnet-sdk-install.exe"
%TEMP%\dotnet-sdk-install.exe /silent
call RefreshEnv.Cmd

dotnet test tests || goto :error

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
