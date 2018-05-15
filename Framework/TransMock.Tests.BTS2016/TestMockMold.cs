using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransMock;

namespace TransMock.Tests.BTS2016
{
    [TestClass]
    public class TestMockMold
    {
        [TestMethod]
        [DeploymentItem(@"TestData\TestFileIn.txt")]
        public void SimpleFlow_HappyPath()
        {
            var integrationMock = new IntegrationMock<TestMockAddresses>();

            integrationMock
                .SetupReceive(a => a.ReceiveFirstMessage_FILE)
                .SetupSend(a => a.SendFirstMessage_FILE);

            var mold = new Mold<TestMockAddresses>(integrationMock);

            mold.WireUp()
                //.Send(r => r.ReceiveFirstMessage_FILE,
                //    ep => {
                //        ep.RequestFilePath = "TestFileIn.txt";
                //        ep.TimeoutInSeconds = 10;
                //        ep.MessageEncoding = System.Text.Encoding.UTF8;
                //        },
                //    ctx => ctx.DebugInfo("Fire in the hall")
                //)
                .Send(r => r.ReceiveFirstMessage_FILE,
                    ep => ep.RequestFilePath = "TestFileIn.txt",
                    ep => ep.MessageEncoding = System.Text.Encoding.UTF8,
                    ep => ep.TimeoutInSeconds = 10,
                    ctx => ctx.DebugInfo("Fire in the hall")
                    )
                .Receive(
                    s => s.SendFirstMessage_FILE, 
                    ep => {
                        ep.TimeoutInSeconds = 10;
                        ep.MessageEncoding = System.Text.Encoding.UTF8;
                    },
                    ctx => ctx.DebugInfo("Yet one more blast!"),
                    (i,v) => { return v.Length > 0; });
        }
    }
}
