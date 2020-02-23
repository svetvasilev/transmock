using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransMock;
using TransMock.Wcf.Adapter.Utils.BizTalkProperties;

namespace BizTalkTests.IntegrationTests
{
    [TestClass]
    public class TestAdapterProperties
    {
        [TestMethod]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        public void TestServiceBusAdapterProperties_HappyPath()
        {
            var serviceMock = new EndpointsMock<BizTalkTestsMockAddresses>();

            serviceMock.SetupReceive(r => r.BTS_OneWayReceive3_SBus)
                .SetupSend(s => s.BTS_OneWayTestSend_SBus);

            var messagingClient = serviceMock.CreateMessagingClient();

            messagingClient.Send(r => r.BTS_OneWayReceive3_SBus,
                "StartMessage.xml",
                messagePropertiesSetter: msp => new Dictionary<string, string>()
                {
                    { "ServiceBus.CorrelationId", "SomeCorrelationID" }
                })
                .Receive(s => s.BTS_OneWayTestSend_SBus,
                    validator: v => ValidateCorrelationId(v, "SomeCorrelationID"));


        }

        private bool ValidateCorrelationId(IndexedMessageReception receivedMessage, string correlationId)
        {
            Assert.IsNotNull(receivedMessage, "The received message is null!");
            Assert.IsNotNull(receivedMessage.Message, "The received message does not have content!");
            Assert.IsTrue(receivedMessage.Index == 0, "The received message has another index as expected!");

            var xDoc = XDocument.Parse(receivedMessage.Message.Body);
            var correlationIdelement = xDoc.Root.Element("CorrelationId");

            Assert.IsNotNull(correlationIdelement, "There is no CorrelationId element in the response!");
            Assert.AreEqual(correlationId, correlationIdelement.Value, "The correlationId is not as expected!");

            return true;

        }
    }
}
