using System;
using Xunit;

using Aws.CRT;

namespace tests
{
    public class EventLoopGroupTest
    {
        [Fact]
        public void TestCreateDestroy()
        {
            EventLoopGroup elg = new EventLoopGroup(1);
        }
    }
}
