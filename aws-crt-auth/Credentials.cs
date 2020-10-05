/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System.Text;

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

        public Credentials(string accessKeyId, string secretAccessKey, string sessionToken)
        {
            AccessKeyId = ASCIIEncoding.ASCII.GetBytes(accessKeyId);
            SecretAccessKey = ASCIIEncoding.ASCII.GetBytes(secretAccessKey);
            if (sessionToken != null) {
                SessionToken = ASCIIEncoding.ASCII.GetBytes(sessionToken);
            }
        }
    }
}
