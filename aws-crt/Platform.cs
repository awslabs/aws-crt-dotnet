/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */

using System;

#if NETSTANDARD
using System.Runtime.InteropServices;
#else
using System.IO;
#endif

namespace Aws.Crt
{
    public enum PlatformOS {
        WINDOWS,
        MAC,
        UNIX
    }

    public class Platform {
#if NETSTANDARD
        public static PlatformOS GetRuntimePlatformOS() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return PlatformOS.WINDOWS;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return PlatformOS.UNIX;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return PlatformOS.MAC;
            }
            else
            {
                throw new CrtException("Could not detect a supported platform");
            }
        }
#else
        public static PlatformOS GetRuntimePlatformOS() {

            /*
            * Taken from https://stackoverflow.com/questions/38790802/determine-operating-system-in-net-core/38795621#38795621
            */
            string windir = Environment.GetEnvironmentVariable("windir");
            if (!string.IsNullOrEmpty(windir) && windir.Contains(@"\") && Directory.Exists(windir))
            {
                return PlatformOS.WINDOWS;
            }
            else if (File.Exists(@"/proc/sys/kernel/ostype"))
            {
                return PlatformOS.UNIX;
            }
            else if (File.Exists(@"/System/Library/CoreServices/SystemVersion.plist"))
            {
                return PlatformOS.MAC;
            }
            else
            {
                throw new CrtException("Could not detect a supported platform");
            }
        }
#endif
    }
}