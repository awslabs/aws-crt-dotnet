/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Aws.Crt
{
    [SecuritySafeCritical]
    public static class CRT
    {
        static bool Is64Bit = (IntPtr.Size == 8);

        [SecuritySafeCritical]
        internal static class API
        {
            public delegate IntPtr aws_dotnet_error_string(int errorCode);
            public delegate IntPtr aws_dotnet_error_name(int errorCode);
            public static aws_dotnet_error_string error_string = NativeAPI.Bind<aws_dotnet_error_string>();
            public static aws_dotnet_error_name error_name = NativeAPI.Bind<aws_dotnet_error_name>();
        }

        public static void CopyStream(Stream source, Stream dest, int destSize)
        {
            byte[] buffer = new byte[4096];
            int copied = 0;
            int maximumRead = buffer.Length;
            if (destSize > 0) {
                maximumRead = Math.Min(maximumRead, destSize);
            }

            int read;
            while ((read = source.Read(buffer, 0, maximumRead)) > 0)
            {
                dest.Write(buffer, 0, read);
                copied += read;
                if (destSize > 0) {
                    maximumRead = Math.Min(destSize - copied, buffer.Length);
                }
            }
        }

        public static string ErrorString(int errorCode)
        {
            return Marshal.PtrToStringAnsi(API.error_string(errorCode));
        }

        public static string ErrorName(int errorCode)
        {
            return Marshal.PtrToStringAnsi(API.error_name(errorCode));
        }

        internal class LibraryHandle : Handle
        {
            private string libraryPath;
            public LibraryHandle(IntPtr value, string path)
            {
                libraryPath = path;
                SetHandle(value);
            }

            protected override bool ReleaseHandle()
            {
                CRT.Loader.FreeLibrary(handle);
                // No longer need the library file, delete it
                try {
                    File.Delete(libraryPath);
                }
                catch (Exception) {
                    // best effort, just ignore it
                }
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
            private string libraryPath;

            public PlatformBinding()
            {
                string libraryName = null;
                string arch = (Is64Bit) ? "x64" : "x86";
                PlatformOS os = Aws.Crt.Platform.GetRuntimePlatformOS();

                switch(os) {
                    case PlatformOS.WINDOWS:
                        libraryName = $"aws-crt-dotnet-{arch}.dll";
                        break;

                    case PlatformOS.UNIX:
                        libraryName = $"libaws-crt-dotnet-{arch}.so";
                        break;

                    case PlatformOS.MAC:
                        libraryName = $"libaws-crt-dotnet-{arch}.dylib";
                        break;
                }

                try
                {
                    libraryPath = ExtractLibrary(libraryName);

                    // Work around virus scanners munching on a newly found DLL
                    int tries = 0;
                    do
                    {
                        crt = CRT.Loader.LoadLibrary(libraryPath);
                        if (crt.IsInvalid)
                        {
                            Thread.Sleep(10);
                        }
                    } while (crt.IsInvalid && tries++ < 100);

                    if (crt.IsInvalid)
                    {
                        string error = CRT.Loader.GetLastError();
                        throw new InvalidOperationException($"Unable to load {libraryPath}: error={error}");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Unable to load {libraryPath}, exception occurred", ex);
                }

                Init();
            }

            ~PlatformBinding()
            {
                Shutdown();
            }

            private string ExtractLibrary(string libraryName)
            {
                var crtAsm = Assembly.GetAssembly(typeof(CRT));
                Stream resourceStream = null;
                try
                {
                    resourceStream = crtAsm.GetManifestResourceStream("Aws.CRT." + libraryName);
                    if (resourceStream == null)
                    {
                        var resources = crtAsm.GetManifestResourceNames();
                        var resourceList = String.Join(",", resources);
                        throw new IOException($"Could not find {libraryName} in resource manifest; Resources={resourceList}");
                    }
                    string prefix = Path.GetRandomFileName();
                    var extractedLibraryPath = Path.GetTempPath() + prefix + "." + libraryName;
                    FileStream libStream = null;
                    // Open the shared lib stream, write the embedded stream to it, and it will be deleted later
                    try
                    {
                        libStream = new FileStream(extractedLibraryPath, FileMode.Create, FileAccess.Write);
                        CopyStream(resourceStream, libStream, 0);
                        return extractedLibraryPath;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Could not extract {libraryName} to {extractedLibraryPath}", ex);
                    }
                    finally
                    {
                        libStream?.Dispose();
                    }
                }
                finally
                {
                    resourceStream?.Dispose();
                }
            }

            private delegate void aws_dotnet_static_init();
            private delegate void aws_dotnet_static_shutdown();

            // This must remain referenced through execution, or the delegate will be garbage collected
            // and a crash will occur on the first exception thrown
            private NativeException.NativeExceptionRecorder recordNativeException = NativeException.RecordNativeException;
            private void Init()
            {
                aws_dotnet_static_init nativeInit = (aws_dotnet_static_init) GetFunction<aws_dotnet_static_init>("aws_dotnet_static_init");
                nativeInit();

                NativeException.SetExceptionCallback setExceptionCallback = (NativeException.SetExceptionCallback) GetFunction<NativeException.SetExceptionCallback>("aws_dotnet_set_exception_callback");
                setExceptionCallback(recordNativeException);
            }

            private void Shutdown()
            {
                aws_dotnet_static_shutdown nativeShutdown = (aws_dotnet_static_shutdown) GetFunction<aws_dotnet_static_shutdown>("aws_dotnet_static_shutdown");
                nativeShutdown();
            }

            public Object GetFunction<DT>(string name)
            {
                IntPtr function = GetFunctionAddress(name);
                if (function == IntPtr.Zero)
                {
                    throw new InvalidOperationException($"Unable to resolve function {name}");
                }
                return Marshal.GetDelegateForFunctionPointer(function, typeof(DT));
            }

            public IntPtr GetFunctionAddress(string name) {
                return CRT.Loader.GetFunction(crt.DangerousGetHandle(), name);
            }
        }

        internal static PlatformBinding Binding { get; private set; } = new PlatformBinding();

        private class WindowsLoader : PlatformLoader
        {
            internal static class kernel32
            {
                [DllImport("kernel32", SetLastError = true)]
                public static extern IntPtr LoadLibrary(string fileName);

                [DllImport("kernel32")]
                public static extern IntPtr GetProcAddress(IntPtr module, string procName);

                [DllImport("kernel32")]
                public static extern int FreeLibrary(IntPtr module);
            }

            public override LibraryHandle LoadLibrary(string name)
            {
                string path = name;
                if (!Path.IsPathRooted(name))
                {
                    Assembly crtAsm = Assembly.GetAssembly(typeof(CRT));
                    path = crtAsm.Location.Replace(crtAsm.GetName().Name + ".dll", name);
                }
                return new LibraryHandle(kernel32.LoadLibrary(path), path);
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
                // Win32Exception will invoke FormatMessage to extract the error string from GetLastError()
                return new Win32Exception(Marshal.GetLastWin32Error()).Message;
            }
        }

        private class GlibcLoader : PlatformLoader
        {
            // Look specifically for libdl.so.2 on linux/glibc platforms
            internal static class glibc
            {
                public const int RTLD_NOW = 0x002;

                [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
                public static extern IntPtr dlopen(string fileName, int flags);

                [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
                public static extern IntPtr dlsym(IntPtr handle, string name);

                [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
                public static extern int dlclose(IntPtr handle);

                [DllImport("libdl.so.2", CallingConvention = CallingConvention.Cdecl)]
                public static extern string dlerror();
            }

            public override LibraryHandle LoadLibrary(string name)
            {
                string path = name;
                if (!Path.IsPathRooted(name))
                {
                    Assembly crtAsm = Assembly.GetAssembly(typeof(CRT));
                    path = crtAsm.Location.Replace(crtAsm.GetName().Name + ".dll", name);
                }
                return new LibraryHandle(glibc.dlopen(path, glibc.RTLD_NOW), path);
            }

            public override void FreeLibrary(IntPtr handle)
            {
                glibc.dlclose(handle);
            }

            public override IntPtr GetFunction(IntPtr handle, string name)
            {
                return glibc.dlsym(handle, name);
            }

            public override string GetLastError()
            {
                return glibc.dlerror();
            }
        }

        private class DarwinLoader : PlatformLoader
        {
            // Darwin shims libdl, so just look for it undecorated/no SOVERSION
            internal static class darwin
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

            public override LibraryHandle LoadLibrary(string name)
            {
                string path = name;
                if (!Path.IsPathRooted(name))
                {
                    Assembly crtAsm = Assembly.GetAssembly(typeof(CRT));
                    path = crtAsm.Location.Replace(crtAsm.GetName().Name + ".dll", name);
                }
                return new LibraryHandle(darwin.dlopen(path, darwin.RTLD_NOW), path);
            }

            public override void FreeLibrary(IntPtr handle)
            {
                darwin.dlclose(handle);
            }

            public override IntPtr GetFunction(IntPtr handle, string name)
            {
                return darwin.dlsym(handle, name);
            }

            public override string GetLastError()
            {
                return darwin.dlerror();
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

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    return s_loader = new WindowsLoader();
                }
                else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    return s_loader = new DarwinLoader();
                }
                else if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    return s_loader = new GlibcLoader();
                }

                return null;
            }
        }

        // Base class for native resources. SafeHandle guarantees that when the handle
        // goes out of scope, the ReleaseHandle() function will be called, which each
        // Handle subclass will implement to free the resource
        public abstract class Handle : SafeHandle
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

    public class CrtException : Exception {

        public int ErrorCode {get; private set; }

        public CrtException(int errorCode) :
            base(String.Format("Crt Runtime Exception: {0}", CRT.ErrorString(errorCode)))
        {
            ErrorCode = errorCode;
        }

        public CrtException(string message) :
            base(message)
        {
        }
    }
}
