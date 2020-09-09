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
    public class Credentials {

        public byte[] AccessKeyId { get; private set; }
        public byte[] SecretAccessKey { get; private set; }
        public byte[] SessionToken { get; private set;}

        public Credentials(byte[] accessKeyId, byte[] secretAccessKey, byte[] sessionToken) 
        {
            AccessKeyId = accessKeyId;
            SecretAccessKey = secretAccessKey;
            SessionToken = sessionToken;
        }
    }
}