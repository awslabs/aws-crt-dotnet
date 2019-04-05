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
using System.Runtime.InteropServices;

namespace Aws.CRT
{
    public static class CRT
    {
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
            [DllImport("kernel32")]
            public static extern IntPtr LoadLibrary(string fileName);

            [DllImport("kernel32")]
            public static extern IntPtr GetProcAddress(IntPtr module, string procName);

            [DllImport("kernel32")]
            public static extern int FreeLibrary(IntPtr module);
        }

        public abstract class PlatformLoader
        {
            public abstract IntPtr LoadLibrary(string name);
            public abstract void FreeLibrary(IntPtr handle);
            public abstract IntPtr GetFunction(IntPtr handle, string name);
        }

        public class PlatformBinding
        {
            private IntPtr crt;

            public PlatformBinding()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    crt = CRT.Loader.LoadLibrary("aws-crt-dotnet.dll");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    crt = CRT.Loader.LoadLibrary("libaws-crt-dotnet.so");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    crt = CRT.Loader.LoadLibrary("libaws-crt-dotnet.dylib");
                }
            }

            public DT GetFunction<DT>(string name)
            {
                IntPtr function = CRT.Loader.GetFunction(crt, name);
                if (function == IntPtr.Zero)
                {
                    throw new InvalidOperationException($"Unable to resolve function {name}");
                }

                return Marshal.GetDelegateForFunctionPointer<DT>(function);
            }
        }

        private static PlatformBinding s_binding;
        public static PlatformBinding Binding
        {
            get
            {
                if (s_binding != null)
                {
                    return s_binding;
                }
                return s_binding = new PlatformBinding();
            }
        }

        private class WindowsLoader : PlatformLoader
        {
            public override IntPtr LoadLibrary(string name)
            {
                Assembly crtAsm = Assembly.GetAssembly(typeof(CRT));
                string path = crtAsm.Location.Replace(crtAsm.GetName().Name + ".dll", name);
                return kernel32.LoadLibrary(path);
            }

            public override void FreeLibrary(IntPtr handle)
            {
                kernel32.FreeLibrary(handle);
            }

            public override IntPtr GetFunction(IntPtr handle, string name)
            {
                return kernel32.GetProcAddress(handle, name);
            }
        }

        private class DlopenLoader : PlatformLoader
        {
            public override IntPtr LoadLibrary(string name)
            {
                Assembly crtAsm = Assembly.GetAssembly(typeof(CRT));
                string path = crtAsm.Location.Replace(crtAsm.GetName().Name + ".dll", name);
                return dl.dlopen(path, dl.RTLD_NOW);
            }

            public override void FreeLibrary(IntPtr handle)
            {
                dl.dlclose(handle);
            }
            public override IntPtr GetFunction(IntPtr handle, string name)
            {
                return dl.dlsym(handle, name);
            }
        }

        private static PlatformLoader s_loaderInstance;
        public static PlatformLoader Loader
        {
            get
            {
                if (s_loaderInstance != null)
                {
                    return s_loaderInstance;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return s_loaderInstance = new WindowsLoader();
                }
                else
                {
                    return s_loaderInstance = new DlopenLoader();
                }
            }
        }

        public static void Init()
        {

        }

        public static void Shutdown()
        {

        }
    }
}
