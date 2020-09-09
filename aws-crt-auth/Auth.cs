/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security;

using Aws.Crt;

namespace Aws.Crt.Auth
{
    [SecuritySafeCritical]
    internal class LibraryHandle
    {
        internal LibraryHandle()
        {
        }

        ~LibraryHandle()
        {
        }
    }

    public class AuthTest {
        public AuthTest() {}
    }
}