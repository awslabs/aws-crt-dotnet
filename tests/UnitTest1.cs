using System;
using Xunit;

using Aws.CRT;

namespace tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            int thing = 42;
            Action doTheThing = CRT.Binding.GetFunction<Action>("DoTheThing");
            doTheThing();
        }
    }
}
