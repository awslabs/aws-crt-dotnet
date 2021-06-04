/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using System.Text;
using Xunit;

using Aws.Crt.Cal;

namespace tests
{
    public class HashTest
    {
        [Fact]
        public void TestSha256Empty()
        {
            Hash sha256 = Hash.sha256();
            byte[] res = sha256.digest();
            byte[] expected = {0xe3,0xb0,0xc4,0x42,0x98,0xfc,0x1c,0x14,0x9a,0xfb,0xf4,0xc8,0x99,0x6f,0xb9,0x24,0x27,0xae,0x41,0xe4,0x64,0x9b,0x93,0x4c,0xa4,0x95,0x99,0x1b,0x78,0x52,0xb8,0x55};
            Assert.Equal(expected, res);
        }

        [Fact]
        public void TestSha256OneShot()
        {
            Hash sha256 = Hash.sha256();
            sha256.update(Encoding.ASCII.GetBytes("abc"));
            byte[] res = sha256.digest();
            byte[] expected = {0xba,0x78,0x16,0xbf,0x8f,0x01,0xcf,0xea,0x41,0x41,0x40,0xde,0x5d,0xae,0x22,0x23,0xb0,0x03,0x61,0xa3,0x96,0x17,0x7a,0x9c,0xb4,0x10,0xff,0x61,0xf2,0x00,0x15,0xad};
            Assert.Equal(expected, res);
        }

        [Fact]
        public void TestSha256Iterated()
        {
            Hash sha256 = Hash.sha256();
            sha256.update(Encoding.ASCII.GetBytes("a"));
            sha256.update(Encoding.ASCII.GetBytes("b"));
            sha256.update(Encoding.ASCII.GetBytes("c"));
            byte[] res = sha256.digest();
            byte[] expected = {0xba,0x78,0x16,0xbf,0x8f,0x01,0xcf,0xea,0x41,0x41,0x40,0xde,0x5d,0xae,0x22,0x23,0xb0,0x03,0x61,0xa3,0x96,0x17,0x7a,0x9c,0xb4,0x10,0xff,0x61,0xf2,0x00,0x15,0xad};
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestSha1Empty()
        {
            Hash sha1 = Hash.sha1();
            byte[] res = sha1.digest();
            byte[] expected = {0xda,0x39,0xa3,0xee,0x5e,0x6b,0x4b,0x0d,0x32,0x55,0xbf,0xef,0x95,0x60,0x18,0x90,0xaf,0xd8,0x07,0x09};
            Assert.Equal(expected, res);
        }

        [Fact]
        public void TestSha1OneShot()
        {
            Hash sha1 = Hash.sha1();
            sha1.update(Encoding.ASCII.GetBytes("abc"));
            byte[] res = sha1.digest();
            byte[] expected = {0xa9,0x99,0x3e,0x36,0x47,0x06,0x81,0x6a,0xba,0x3e,0x25,0x71,0x78,0x50,0xc2,0x6c,0x9c,0xd0,0xd8,0x9d};
            Assert.Equal(expected, res);
        }

        [Fact]
        public void TestSha1Iterated()
        {
            Hash sha1 = Hash.sha1();
            sha1.update(Encoding.ASCII.GetBytes("a"));
            sha1.update(Encoding.ASCII.GetBytes("b"));
            sha1.update(Encoding.ASCII.GetBytes("c"));
            byte[] res = sha1.digest();
            byte[] expected = {0xa9,0x99,0x3e,0x36,0x47,0x06,0x81,0x6a,0xba,0x3e,0x25,0x71,0x78,0x50,0xc2,0x6c,0x9c,0xd0,0xd8,0x9d};
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestMd5Empty()
        {
            Hash md5 = Hash.md5();
            byte[] res = md5.digest();
            byte[] expected = {0xd4,0x1d,0x8c,0xd9,0x8f,0x00,0xb2,0x04,0xe9,0x80,0x09,0x98,0xec,0xf8,0x42,0x7e};
            Assert.Equal(expected, res);
        }

        [Fact]
        public void TestMd5OneShot()
        {
            Hash md5 = Hash.md5();
            md5.update(Encoding.ASCII.GetBytes("abc"));
            byte[] res = md5.digest();
            byte[] expected = {0x90,0x01,0x50,0x98,0x3c,0xd2,0x4f,0xb0,0xd6,0x96,0x3f,0x7d,0x28,0xe1,0x7f,0x72};
            Assert.Equal(expected, res);
        }

        [Fact]
        public void TestMd5Iterated()
        {
            Hash md5 = Hash.md5();
            md5.update(Encoding.ASCII.GetBytes("a"));
            md5.update(Encoding.ASCII.GetBytes("b"));
            md5.update(Encoding.ASCII.GetBytes("c"));
            byte[] res = md5.digest();
            byte[] expected = {0x90,0x01,0x50,0x98,0x3c,0xd2,0x4f,0xb0,0xd6,0x96,0x3f,0x7d,0x28,0xe1,0x7f,0x72};
            Assert.Equal(expected, res);
        }
    }
}
