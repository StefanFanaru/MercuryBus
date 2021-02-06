using System.Collections.Generic;
using MercuryBus.Events.Common;
using MercuryBus.IntegrationTests.TestHelpers;
using MercuryBus.Local.Kafka.Consumer;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;

namespace MercuryBus.IntegrationTests.TestFixtures
{
    [TestFixture]
    public class BadSchemaIntegrationTests : IntegrationTestsBase
    {
        [SetUp]
        public void Setup()
        {
            TestSetup("badschema", false, MercuryKafkaConsumerConfigurationProperties.Empty());
        }

        [TearDown]
        public void TearDown()
        {
            DisposeTestHost();
        }

        [Test]
        public void Publish_DatabaseSchemaNotCreated_ThrowsException()
        {
            // Arrange
            TestMessageType1 msg1 = new TestMessageType1("Msg1", 1, 1.2);
            TestEventConsumer consumer = GetTestConsumer();

            // Act
            Assert.Throws<DbUpdateException>(delegate ()
            {
                GetTestPublisher().Publish(AggregateType12, AggregateType12, new List<IDomainEvent> { msg1 });
            });
        }
    }
}
