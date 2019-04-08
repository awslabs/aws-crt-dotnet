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
            // When elg goes out of scope, the native handle will be released
        }
    }
}
