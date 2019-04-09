using System;
using Xunit;

using Aws.CRT;

namespace tests
{
    class Test {
        public class Interface {
            public delegate int aws_test_exception(int a, int b);

            public aws_test_exception test;
        }

        public static Interface API = NativeAPI.Resolve<Interface>();
    }
    public class ExceptionTest
    {
        [Fact]
        public void TestException()
        {
            int result = 0;
            bool threwException = false;
            try {
                result = Test.API.test(42, 1);
            } catch (NativeException ex) {
                threwException = true;
            }

            Assert.True(threwException);
            Assert.Equal(0, result);
        }
    }
}
