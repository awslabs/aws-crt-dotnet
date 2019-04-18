
:: Downloads and installs dotnet 2.2 SDK
:: valid urls for %1:
:: 64-bit: https://download.visualstudio.microsoft.com/download/pr/3c43f486-2799-4454-851c-fa7a9fb73633/673099a9fe6f1cac62dd68da37ddbc1a/dotnet-sdk-2.2.203-win-x64.exe
:: 32-bit: https://download.visualstudio.microsoft.com/download/pr/df174ab6-0fcd-47cd-bc95-6a0e09e8f71b/fc7101af6ac2cdac1e0a09075490fd45/dotnet-sdk-2.2.203-win-x86.exe

"%SystemRoot%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "Invoke-WebRequest %1 -OutFile %TEMP%\dotnet-sdk-install.exe" || goto :error
%TEMP%\dotnet-sdk-install.exe /install /quiet /norestart || goto :error

"%SystemRoot%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "Invoke-WebRequest http://www.dependencywalker.com/depends22_x64.zip -OutFile %TEMP%\depends.zip" || goto :error
powershell Expand-Archive %TEMP%\depends.zip -DestinationPath %TEMP%\depends || goto :error

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
