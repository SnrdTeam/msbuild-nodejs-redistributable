using System;
using Xunit;

namespace TestWebApp.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Ok()
        {
            Assert.True(true);
        }

        [Fact]
        public void NotOk()
        {
            Assert.True(false);
        }
    }
}
