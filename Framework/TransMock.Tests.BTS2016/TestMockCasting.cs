using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransMock;

namespace TransMock.Tests.BTS2016
{
    [TestClass]
    public class TestMockCasting
    {
        [TestMethod]
        public void SetupReceive()
        {
            // var casting = new TestCasting<TestMockAddresses>();

            // casting.SetupReceive(r => r.ReceiveFirstMessage_FILE);
            //CheckSetupForStatic(() => TestMockAddresses.ReceiveFirstMessage_FILE);
            CheckSetupForStatic(r => r.ReceiveFirstMessage_FILE);
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestFileIn.txt")]
        public void SimpleFlowTest_OneWayIn_OneWayOut()
        {
            var castingMock = new TestCasting<TestMockAddresses>();

            // Here the syntax is a bit confusing - we setup a receive for a corresponding send port
            // If the intention is to retain the semantics of setting up receive for a corresponding receive location
            // then the implementation of the method should be reversed - it shuold implement a send operation, and vice versa.
            castingMock
                .SetupReceive(r => r.ReceiveFirstMessage_FILE)
                .SetupSend(s => s.SendFirstMessage_FILE);

            var mold = new TestMold<TestMockAddresses>(castingMock);
            // What if we use the already set up receive endpoint instead of creating a new one. 
            // It creates even further confusion
            // Something like mold.Send( adr => adr.ReceiveFirstMessage_FILE, 
            //      op => { op.MessageEncoding = System.Text.Encoding.UTF8; op.RequestFilePath = "TestFileIn.txt"},
            //      ctx => ctx.LogInfo("Sending message to the integration"))
            mold.Send((c,ma) => new ReceiveEndpoint()
                {
                    URL = ma.ReceiveFirstMessage_FILE,
                    MessageEncoding = System.Text.Encoding.UTF8,
                    RequestFilePath = "TestFileIn.txt",
                    TimeoutInSeconds = 10                   
                }
            )
            .Receive((c,ma) => new SendEndpoint()
                {
                    URL = ma.SendFirstMessage_FILE,
                    TimeoutInSeconds = 10
                },
                // TODO: Validation helpers for performing the reading and looking up inside messages
                //       a piece of joyful cake
                (i,v) =>
                {
                    string fileContents;
                    using (var sr = new System.IO.StreamReader(v))
                    {
                        fileContents = sr.ReadToEnd();
                    }

                    Assert.AreEqual("This is a test file", fileContents, "The contents of the received file differs");

                    return true;
                });
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestFileIn.txt")]
        public void SimpleFlowTest_OneWayIn_OneWayOut_Alt()
        {
            var castingMock = new TestCasting<TestMockAddresses>();

            // Here the syntax is a bit confusing - we setup a receive for a corresponding send port
            // If the intention is to retain the semantics of setting up receive for a corresponding receive location
            // then the implementation of the method should be reversed - it shuold implement a send operation, and vice versa.
            castingMock
                .SetupReceive(r => r.ReceiveFirstMessage_FILE)
                .SetupSend(s => s.SendFirstMessage_FILE);

            var mold = new TestMold<TestMockAddresses>(castingMock);
            // What if we use the already set up receive endpoint instead of creating a new one. 
            // It creates even further confusion
            // Something like mold.Send( adr => adr.ReceiveFirstMessage_FILE, 
            //      op => { op.MessageEncoding = System.Text.Encoding.UTF8; op.RequestFilePath = "TestFileIn.txt"},
            //      ctx => ctx.LogInfo("Sending message to the integration"))
            mold.
                Send(
                    ra => ra.ReceiveFirstMessage_FILE,
                    ep => {
                        ep.MessageEncoding = System.Text.Encoding.UTF8;
                        ep.RequestFilePath = "TestFileIn.txt";
                        ep.TimeoutInSeconds = 10;
                        },
                    c => c.DebugInfo("Sending message to endoint 0")
                )
                //Send((c, ma) => new ReceiveEndpoint()
                //{
                //    URL = ma.ReceiveFirstMessage_FILE,
                //    MessageEncoding = System.Text.Encoding.UTF8,
                //    RequestFilePath = "TestFileIn.txt",
                //    TimeoutInSeconds = 10
                //}
                //)
                .Receive(
                    sa => sa.SendFirstMessage_FILE, 
                    ep => { ep.TimeoutInSeconds = 10; ep.MessageEncoding = Encoding.UTF8; },
                    c => c.DebugInfo("Calling endpoint 1"),
                    (i, v) =>
                    {
                        string fileContents;
                        using (var sr = new System.IO.StreamReader(v))
                        {
                            fileContents = sr.ReadToEnd();
                        }

                        Assert.AreEqual("This is a test file", fileContents, "The contents of the received file differs");

                        return true;
                    }
                );
            //.Receive(
            //    (c, ma) => new SendEndpoint()
            //{
            //    URL = ma.SendFirstMessage_FILE,
            //    TimeoutInSeconds = 10
            //},
            //    // TODO: Validation helpers for performing the reading and looking up inside messages
            //    //       a piece of joyful cake
            //    (i, v) =>
            //    {
            //        string fileContents;
            //        using (var sr = new System.IO.StreamReader(v))
            //        {
            //            fileContents = sr.ReadToEnd();
            //        }

            //        Assert.AreEqual("This is a test file", fileContents, "The contents of the received file differs");

            //        return true;
            //    });
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestFileRequest.txt")]
        [DeploymentItem(@"TestData\TestFileResponse.txt")]
        public void SimpleFlowTest_TwoWayIn_TwoWayOut()
        {
            var castingMock = new TestCasting<TestMockAddresses>();

            // Here the syntax is a bit confusing - we setup a receive to a receive location
            // If the intention is to retain the semantics of setting up receive for a corresponding receive location
            // then the implementation of the method should be reversed - it shuold implement a send operation, and vice versa.
            castingMock
                .SetupSendRequestAndReceiveResponse(r => r.TwoWayReceive_WebHTTP)
                .SetupReceiveRequestAndSendResponse(s => s.TwoWaySend_WebHTTP);

            var mold = new TestMold<TestMockAddresses>(castingMock);

            mold.InParallel((tm) =>
               tm.ReceiveRequestAndSendResponse(
                   (ctx, adr) => new SendEndpoint()
                   {
                       URL = adr.TwoWaySend_WebHTTP,
                       MessageEncoding = System.Text.Encoding.UTF8,
                       // DONE: This serves well for static responses, but one of the main challanges
                       // with the BizUnit based implementation is the need to identify and send content
                       // dynamically based on the contents of the request
                       //ResponseFilePath = "TestFileResponse.txt",
                       TimeoutInSeconds = 10
                   },
                   (i,v) =>
                   {
                       string fileContents;
                       using (var sr = new System.IO.StreamReader(v))
                       {
                           fileContents = sr.ReadToEnd();
                       }

                       // TODO: Handle errors correctly in parallel scenarios
                       //       In case a validation fails here, the process hangs
                       Assert.AreEqual("This is a test request file", fileContents, "The contents of the received file differs");
                       return true;
                   },
                   // TODO: consider to set the response strategy in the first parameter
                   // by specifying it as a property on a composit operation config object
                   resp =>
                   {
                       var strategy = new StaticFileResponseStrategy();
                       strategy.FilePath = "TestFileResponse.txt";

                       return strategy;
                   }
                )
            ) // End of parallel actions
            .SendRequestAndReceiveResponse(
                (c, ma) => new ReceiveEndpoint()
                {
                    URL = ma.TwoWayReceive_WebHTTP,
                    MessageEncoding = System.Text.Encoding.UTF8,
                    RequestFilePath = "TestFileRequest.txt",
                    TimeoutInSeconds = 10
                },
                v =>
                {
                    string fileContents;
                    using (var sr = new System.IO.StreamReader(v))
                    {
                        fileContents = sr.ReadToEnd();
                    }

                    Assert.AreEqual("This is a test response file", fileContents, "The contents of the received file differs");

                    return true;
                }
            )
            .CleanUp(); // We call cleanup at the end
        }

        private void CheckSetupForStatic(Func<TestMockAddresses,string> expression)
        {
            var mockAddresses = new TestMockAddresses();
            string value = expression(mockAddresses);

            System.Diagnostics.Trace.WriteLine(string.Format("returned string was: {0}", value));
        } 
    }
}
