
:: install dotnet
choco install dotnetcore-sdk -y

dotnet test tests || goto :error

goto :EOF

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
