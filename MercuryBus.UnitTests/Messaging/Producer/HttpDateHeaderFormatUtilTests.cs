using System;
using System.Collections.Generic;
using System.Text;
using MercuryBus.Messaging.Producer;
using NUnit.Framework;

namespace MercuryBus.UnitTests.Messaging.Producer
{
    public class HttpDateHeaderFormatUtilTests
    {
        [Test]
        public void NowAsHttpDateString_GetResult_NotNull()
        {
            Assert.NotNull((HttpDateHeaderFormatUtil.NowAsHttpDateString()));
        }
    }
}
