## AWS Common Runtime for .NET

.NET bindings for the AWS Common Runtime

## License

This library is licensed under the Apache 2.0 License.

## Developer Guidance

### Pre-requirement

- Install .NET. Note: the default version is .NET 6.0 now, but we test on .NET 5.0 and .NET Core 3.1. So, install from [here](https://dotnet.microsoft.com/en-us/download/dotnet) for different version.

- Clean up the work directory. .NET will not override the previous build. You can use the script below to clean the previous build:

<details>
<summary>(clean_up.sh)</summary>
<pre>
#!/bin/bash

rm -rf build
rm -rf packages
rm -rf aws-crt/bin
rm -rf aws-crt/obj
rm -rf aws-crt-http/bin
rm -rf aws-crt-http/obj
rm -rf aws-crt-auth/bin
rm -rf aws-crt-auth/obj
rm -rf aws-crt-checksums/bin
rm -rf aws-crt-checksums/obj
rm -rf aws-crt-cal/bin
rm -rf aws-crt-cal/obj
rm -rf ~/.nuget/packages/awscrt*
rm -rf tests/bin
rm -rf tests/obj
rm -rf tools/Elasticurl/bin
rm -rf tools/Elasticurl/ob
</pre>
</details>

### Build and Test steps

- `dotnet build -f netstandard2.0 -p:PlatformTarget=x64 -p:CMakeConfig=Debug` for 64 bit mac machine. Check ./builder.json for `PlatformTarget` on your machine.
- `dotnet pack -p:TargetFrameworks=netstandard2.0`
- `dotnet test tests`. For test a single test: `dotnet test tests --filter DisplayName~<ClassName/MethodName>`. Check [doc](https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=xunit) for details.

## Mac-Only TLS Behavior

Please note that on Mac, once a private key is used with a certificate, that certificate-key pair is imported into the Mac Keychain.  All subsequent uses of that certificate will use the stored private key and ignore anything passed in programmatically.  Beginning in v0.3.5, When a stored private key from the Keychain is used, the following will be logged at the "info" log level:

```
static: certificate has an existing certificate-key pair that was previously imported into the Keychain.  Using key from Keychain instead of the one provided.
```
