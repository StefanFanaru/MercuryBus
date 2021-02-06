using System;
using MercuryBus.Events.Common;

namespace MercuryBus.IntegrationTests.TestHelpers
{
    public class TestMessageUnsubscribedType : IDomainEvent
    {
        public String Name { get; set; }
        public TestMessageUnsubscribedType(String name)
        {
            Name = name;
        }
    }
}
