using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransMock;
using TransMock.Addressing;

namespace TransMock.Tests
{
    [TestClass]
    public class TestSimpleMock
    {
        [TestMethod]
        [DeploymentItem(@"TestData\TestFileIn.txt")]
        public void SimpleFlow_HappyPath()
        {
            var integrationMock = new EndpointsMock<TestMockAddresses>();

            integrationMock
                .SetupReceive(a => a.ReceiveFirstMessage_FILE)
                .SetupSend(a => a.SendFirstMessage_FILE);

            var emulator = integrationMock.CreateMessagingClient();

            emulator
                .Send(r => r.ReceiveFirstMessage_FILE,
                    "TestFileIn.txt",
                    System.Text.Encoding.UTF8,
                    10,
                    beforeSendAction: ctx => ctx.DebugInfo("Fire in the hall")
                 )
                .Receive(
                    s => s.SendFirstMessage_FILE,                                         
                    beforeReceiveAction: ctx => ctx.DebugInfo("Yet one more blast!"),
                    validator: v => { return v.Message.Body.Length > 0; });
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestFileIn.txt")]
        public void SimpleFlow_PromotedProperties_HappyPath()
        {
            var integrationMock = new EndpointsMock<TestMockAddresses>();

            integrationMock
                .SetupReceive(a => a.ReceiveFirstMessage_FILE)
                .SetupSend(a => a.SendFirstMessage_FILE);

            var emulator = integrationMock.CreateMessagingClient();

            emulator
                .Send(r => r.ReceiveFirstMessage_FILE,
                    "TestFileIn.txt",
                    System.Text.Encoding.UTF8,
                    10,
                    messagePropertiesSetter: p => p.Add(
                        "BTS.RequestFileName", "SomeFIleName.fln"),
                    beforeSendAction: ctx => ctx.DebugInfo("Fire in the hall")
                 )
                .Receive(
                    s => s.SendFirstMessage_FILE,
                    beforeReceiveAction: ctx => ctx.DebugInfo("Yet one more blast!"),
                    validator: v => { return v.Message.Body.Length > 0; });
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestFileRequest.txt")]
        [DeploymentItem(@"TestData\TestFileResponse.txt")]
        public void Simple2WayFlow_HappyPath()
        {
            var integrationMock = new EndpointsMock<TestMockAddresses>();

            integrationMock.
                SetupSendRequestAndReceiveResponse(
                    r => r.TwoWaySend_WebHTTP)
                .SetupReceiveRequestAndSendResponse(
                    s => s.TwoWayReceive_WebHTTP);

            var messageClient = integrationMock.CreateMessagingClient();

            messageClient.InParallel(
                 (m) => m.ReceiveRequestAndSendResponse(
                     s => s.TwoWaySend_WebHTTP,
                     rs => new StaticFileResponseStrategy()
                     {
                         FilePath = "TestFileResponse.txt"
                     },
                     requestValidator: v =>
                     {
                         Assert.IsTrue(v.Message.Body.Length > 0, "The received request is empty!");
                         Assert.IsTrue(
                             v.Message.Body.Equals("This is a test request file",
                                StringComparison.InvariantCulture),
                             "The contents of the request message is not the same");
                         return true;
                     }
                )
            )
            .SendRequestAndReceiveResponse(
                r => r.TwoWayReceive_WebHTTP,
                "TestFileRequest.txt",
                responseValidator: v =>
                {
                    Assert.IsTrue(v.Body.Length > 0, "The response message is empty");
                    Assert.AreEqual(
                        "This is a test response file",
                        v.Body,
                             "The contents of the response message is not the same");
                    return true;
                }
            )
            .VerifyParallel();

        }
    }
}
