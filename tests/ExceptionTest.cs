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
    public class ExceptionTest
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
