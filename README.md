## AWS Common Runtime for .NET

.NET bindings for the AWS Common Runtime

## License

This library is licensed under the Apache 2.0 License.

## Developer Guidance

### Pre-requirement

* Install .NET. Note: the default version is .NET 6.0 now, but we test on .NET 5.0 and .NET Core 3.1. So, install from [here](https://dotnet.microsoft.com/en-us/download/dotnet) for different version.

* Clean up the work directory. .NET will not override the previous build. You can us `./clean_rebuild.sh` or `clean_rebuild.bat`, which will clean up the previous build and rebuild the project from source for you.

### Build steps

* `dotnet build -f netstandard2.0 -p:PlatformTarget=x64 -p:CMakeConfig=Debug` for 64 bit mac machine. Check ./builder.json for `PlatformTarget` on your machine.
* `dotnet pack -p:TargetFrameworks=netstandard2.0`
* OR run `chmod a+x ./clean_rebuild.sh && ./clean_rebuild.sh`, which will clean up the previous build and rebuild the project from source.

### Test steps

* Run all tests together: `dotnet test tests`.
* Run a single test: `dotnet test tests --filter DisplayName~<ClassName/MethodName>`. Check [doc](https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=xunit) for details.

## Mac-Only TLS Behavior

Please note that on Mac, once a private key is used with a certificate, that certificate-key pair is imported into the Mac Keychain.  All subsequent uses of that certificate will use the stored private key and ignore anything passed in programmatically.  Beginning in v0.3.5, When a stored private key from the Keychain is used, the following will be logged at the "info" log level:

```c
static: certificate has an existing certificate-key pair that was previously imported into the Keychain.  Using key from Keychain instead of the one provided.
```
