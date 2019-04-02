using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransMock;

namespace TransMock.Tests
{
    [TestClass]
    public class TestSimpleMoc
    {
        [TestMethod]
        [DeploymentItem(@"TestData\TestFileIn.txt")]
        public void SimpleFlow_HappyPath()
        {
            var integrationMock = new EndpointsMock<TestMockAddresses>();

            integrationMock
                .SetupReceive(a => a.ReceiveFirstMessage_FILE)
                .SetupSend(a => a.SendFirstMessage_FILE);

            var emulator = integrationMock.CreateMessagingPatternEmulator();

            emulator
                .Send(r => r.ReceiveFirstMessage_FILE,
                    "TestFileIn.txt",
                    System.Text.Encoding.UTF8,
                    10,
                    ctx => ctx.DebugInfo("Fire in the hall")
                    )
                .Receive(
                    s => s.SendFirstMessage_FILE, 
                    ep => {
                        ep.TimeoutInSeconds = 10;
                        ep.MessageEncoding = System.Text.Encoding.UTF8;
                    },
                    ctx => ctx.DebugInfo("Yet one more blast!"),
                    (i,v) => { return v.Body.Length > 0; });
        }
    }
}
