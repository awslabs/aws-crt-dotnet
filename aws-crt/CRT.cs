/*
 * Copyright 2010-2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// Make internal classes and members in this assembly available to the unit tests
[assembly: InternalsVisibleTo("tests")]
[assembly: InternalsVisibleTo("aws-crt-http")]
[assembly: InternalsVisibleTo("aws-crt-mqtt")]

namespace Aws.Crt
{
    [SecuritySafeCritical]
    public static class CRT
    {
        [SecuritySafeCritical]
        internal static class API
        {
            public delegate IntPtr aws_dotnet_error_string(int errorCode);
            public delegate IntPtr aws_dotnet_error_name(int errorCode);
            public static aws_dotnet_error_string error_string = NativeAPI.Bind<aws_dotnet_error_string>();
            public static aws_dotnet_error_name error_name = NativeAPI.Bind<aws_dotnet_error_name>();
        }

        public static string ErrorString(int errorCode)
        {
            return Marshal.PtrToStringAnsi(API.error_string(errorCode));
        }

        public static string ErrorName(int errorCode)
        {
            return Marshal.PtrToStringAnsi(API.error_name(errorCode));
        }

        // This will only ever be instantiated on dlopen platforms
        internal static class dl
        {
            public const int RTLD_NOW = 0x002;

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr dlopen(string fileName, int flags);

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr dlsym(IntPtr handle, string name);

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
            public static extern int dlclose(IntPtr handle);

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl)]
            public static extern string dlerror();
        }

        // This will only ever be instantiated on Windows/XboxOne
        internal static class kernel32
        {
            [DllImport("kernel32", SetLastError=true)]
            public static extern IntPtr LoadLibrary(string fileName);

            [DllImport("kernel32")]
            public static extern IntPtr GetProcAddress(IntPtr module, string procName);

            [DllImport("kernel32")]
            public static extern int FreeLibrary(IntPtr module);
        }

        internal class LibraryHandle : Handle
        {
            public LibraryHandle(IntPtr value) 
            {
                SetHandle(value);
            }

            protected override bool ReleaseHandle()
            {
                CRT.Loader.FreeLibrary(handle);
                return true;
            }
        }

        internal abstract class PlatformLoader
        {
            public abstract LibraryHandle LoadLibrary(string name);
            public abstract void FreeLibrary(IntPtr handle);
            public abstract IntPtr GetFunction(IntPtr handle, string name);
            public abstract string GetLastError();
        }

        internal class PlatformBinding
        {
            private LibraryHandle crt;

            public PlatformBinding()
            {
                string libraryName = null;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    libraryName = "aws-crt-dotnet.dll";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    libraryName = "libaws-crt-dotnet.so";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    libraryName = "libaws-crt-dotnet.dylib";
                }

                crt = CRT.Loader.LoadLibrary(libraryName);
                if (crt.IsInvalid)
                {
                    string error = CRT.Loader.GetLastError();
                    throw new InvalidOperationException($"Unable to load {libraryName}: error={error}");
                }

                Init();
            }

            ~PlatformBinding()
            {
                Shutdown();
            }

            private delegate void aws_dotnet_static_init();
            private delegate void aws_dotnet_static_shutdown();

            // This must remain referenced through execution, or the delegate will be garbage collected
            // and a crash will occur on the first exception thrown
            private NativeException.NativeExceptionRecorder recordNativeException = NativeException.RecordNativeException;
            private void Init()
            {
                var nativeInit = GetFunction<aws_dotnet_static_init>("aws_dotnet_static_init");
                nativeInit();

                var setExceptionCallback = GetFunction<NativeException.SetExceptionCallback>("aws_dotnet_set_exception_callback");
                setExceptionCallback(recordNativeException);
            }

            private void Shutdown()
            {
                var nativeShutdown = GetFunction<aws_dotnet_static_shutdown>("aws_dotnet_static_shutdown");
                nativeShutdown();
            }

            public DT GetFunction<DT>(string name)
            {
                IntPtr function = GetFunctionAddress(name);
                if (function == IntPtr.Zero)
                {
                    throw new InvalidOperationException($"Unable to resolve function {name}");
                }

                return Marshal.GetDelegateForFunctionPointer<DT>(function);
            }

            public IntPtr GetFunctionAddress(string name) {
                return CRT.Loader.GetFunction(crt.DangerousGetHandle(), name);
            }
        }

        internal static PlatformBinding Binding { get; private set; } = new PlatformBinding();

        private class WindowsLoader : PlatformLoader
        {
            public override LibraryHandle LoadLibrary(string name)
            {
                Assembly crtAsm = Assembly.GetAssembly(typeof(CRT));
                Console.WriteLine("ASM LOCATION: {0}", crtAsm.Location);
                string path = crtAsm.Location.Replace(crtAsm.GetName().Name + ".dll", name);
                Console.WriteLine("DLL LOCATION: {0}", path);
                return new LibraryHandle(kernel32.LoadLibrary(path));
            }

            public override void FreeLibrary(IntPtr handle)
            {
                kernel32.FreeLibrary(handle);
            }

            public override IntPtr GetFunction(IntPtr handle, string name)
            {
                return kernel32.GetProcAddress(handle, name);
            }

            public override string GetLastError()
            {
                return Marshal.GetLastWin32Error().ToString();
            }
        }

        private class DlopenLoader : PlatformLoader
        {
            public override LibraryHandle LoadLibrary(string name)
            {
                Assembly crtAsm = Assembly.GetAssembly(typeof(CRT));
                string path = crtAsm.Location.Replace(crtAsm.GetName().Name + ".dll", name);
                return new LibraryHandle(dl.dlopen(path, dl.RTLD_NOW));
            }

            public override void FreeLibrary(IntPtr handle)
            {
                dl.dlclose(handle);
            }
            
            public override IntPtr GetFunction(IntPtr handle, string name)
            {
                return dl.dlsym(handle, name);
            }

            public override string GetLastError()
            {
                return dl.dlerror();
            }
        }

        private static PlatformLoader s_loader = null;
        internal static PlatformLoader Loader 
        {
            get {
                if (s_loader != null)
                {
                    return s_loader;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return s_loader = new WindowsLoader();
                }
                else
                {
                    return s_loader = new DlopenLoader();
                }
            }
        }

        // Base class for native resources. SafeHandle guarantees that when the handle
        // goes out of scope, the ReleaseHandle() function will be called, which each
        // Handle subclass will implement to free the resource
        internal abstract class Handle : SafeHandle
        {
            protected Handle()
            : base((IntPtr)0, true)
            {
            }

            public override bool IsInvalid
            {
                get
                {
                    return handle == (IntPtr)0;
                }
            }
        }
    }
}
