/**
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: Apache-2.0.
 */
using System;
using Xunit;

using Aws.Crt;

namespace tests
{
    class Test {
        internal static class API
        {
            public delegate int aws_test_exception(int a, int b);
            public delegate void aws_test_exception_void();

            public static aws_test_exception test = NativeAPI.Bind<aws_test_exception>();
            public static aws_test_exception_void test_void = NativeAPI.Bind<aws_test_exception_void>();
        }
    }
    public class ExceptionTest : BaseTest
    {
        [Fact]
        public void EnsureExceptionThrown()
        {
            int result = 0;
            Assert.Throws<NativeException>(() => result = Test.API.test(42, 1));
            Assert.Equal(0, result);
        }

        [Fact]
        public void EnsureExceptionThrownVoid()
        {
            Assert.Throws<NativeException>(() => { Test.API.test_void(); });
        }
    }
}
