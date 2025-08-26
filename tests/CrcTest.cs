/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt.Checksums;

namespace tests
{
    public class CrcTest : BaseTest
    {
        [Fact]
        public void TestCrc32Zeroes()
        {
            byte[] zeroes = new byte[32];
            uint res = Crc.crc32(zeroes);
            uint expected = 0x190A55AD;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc32ZeroesIterated()
        {
            uint res = 0;
            for (int i = 0; i < 32; i++) {
                res = Crc.crc32(new byte[1], res);
            }
            uint expected = 0x190A55AD;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc32Values()
        {
            byte[] values = new byte[32];
            for (byte i = 0; i < 32; i++) {
                values[i] = i;
            }
            uint res = Crc.crc32(values);
            uint expected = 0x91267E8A;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc32ValuesIterated()
        {
            uint res = 0;
            for (byte i = 0; i < 32; i++) {
                byte[] buf = {i};
                res = Crc.crc32(buf, res);
            }
            uint expected = 0x91267E8A;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc32LargeBuffer()
        {
            byte[] zeroes = new byte[25 * (1 << 20)];
            uint res = Crc.crc32(zeroes);
            uint expected = 0x72103906;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc32cZeroes()
        {
            byte[] zeroes = new byte[32];
            uint res = Crc.crc32c(zeroes);
            uint expected = 0x8A9136AA;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc32cZeroesIterated()
        {
            uint res = 0;
            for (int i = 0; i < 32; i++) {
                res = Crc.crc32c(new byte[1], res);
            }
            uint expected = 0x8A9136AA;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc32cValues()
        {
            byte[] values = new byte[32];
            for (byte i = 0; i < 32; i++) {
                values[i] = i;
            }
            uint res = Crc.crc32c(values);
            uint expected = 0x46DD794E;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc32cValuesIterated()
        {
            uint res = 0;
            for (byte i = 0; i < 32; i++) {
                byte[] buf = {i};
                res = Crc.crc32c(buf, res);
            }
            uint expected = 0x46DD794E;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc32cLargeBuffer()
        {
            byte[] zeroes = new byte[25 * (1 << 20)];
            uint res = Crc.crc32c(zeroes);
            uint expected = 0xfb5b991d;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc64NVMEZeroes()
        {
            byte[] zeroes = new byte[32];
            ulong res = Crc.crc64nvme(zeroes);
            ulong expected = 0xCF3473434D4ECF3B;
            Assert.Equal(expected, res);
        }
        [Fact]
        public void TestCrc64NVMEZeroesIterated()
        {
            ulong res = 0;
            for (int i = 0; i < 32; i++) {
                res = Crc.crc64nvme(new byte[1], res);
            }
            ulong expected = 0xCF3473434D4ECF3B;
            Assert.Equal(expected, res);
        }
    }
}
