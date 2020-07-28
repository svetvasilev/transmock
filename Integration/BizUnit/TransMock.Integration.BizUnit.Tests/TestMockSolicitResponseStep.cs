/***************************************
//   Copyright 2014 - Svetoslav Vasilev

//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//     http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
*****************************************/

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ServiceModel.Channels.Common;

using System.IO;
using System.IO.Pipes;
using System.ServiceModel;
using System.ServiceModel.Channels;

using TransMock.TestUtils;
using TransMock.Wcf.Adapter;
using TransMock.Integration.BizUnit;

using BizUnit;
using Moq;
using BizUnit.Core.Utilites;
using BizUnit.Core.TestBuilder;

namespace TransMock.Integration.BizUnit.Tests
{
    /// <summary>
    /// Tests the MockReceiveStep class
    /// </summary>
    [TestClass]
    public class TestMockSolicitResponseStep
    {
        int endpointId = 0;
        public TestMockSolicitResponseStep()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        MockAdapterConnectionUri connectionUri;
        MockAdapter adapter;
        MockAdapterInboundHandler inboundHandler;

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            //Setting up the inbound handler with all the references
            connectionUri = new MockAdapterConnectionUri(
                new Uri(
                    string.Format("mock://localhost/2WayTestEndpoint{0}", endpointId++))
                    );
            adapter = new MockAdapter();
            adapter.Encoding = "UTF-8";
            MockAdapterConnectionFactory connectionFactory = new MockAdapterConnectionFactory(
                connectionUri, null, adapter);
            MockAdapterConnection connection = new MockAdapterConnection(connectionFactory);
            inboundHandler = new MockAdapterInboundHandler(connection, null);

            inboundHandler.StartListener(null, TimeSpan.FromMinutes(1));
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup() 
        {
            if (inboundHandler != null)
                inboundHandler.StopListener(TimeSpan.FromSeconds(10));
            //give some time for the pipe to clean
            System.Threading.Thread.Sleep(100);
        }
        //
        #endregion

        [TestMethod]
        public void TestValidateMethod_ValidStep()
        {
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);
            MockSolicitResponseStep step = new MockSolicitResponseStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.RequestPath = "TestRequest.xml";
            step.Encoding = "UTF-8";
            //Colling Validate in order to start the 
            step.Validate(context);
            step = null;
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestRequest.xml")]
        [DeploymentItem(@"TestData\TestResponse.xml")]
        public void TestSendSmallMessage_XML()
        {            
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();
            
            Context context = new Context(loggerMock.Object);

            MockSolicitResponseStep step = new MockSolicitResponseStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.RequestPath = "TestRequest.xml";
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            //Validating the test step
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            Message msg = null;
            IInboundReply reply;
            //Creating the reply message
            Message msgReply = GeneralTestHelper.CreateMessageWithBase64EncodedBody(
                ReadRequestFileContent("TestResponse.xml"), Encoding.UTF8);

            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                //Now we read the message in the inbound handler                
                inboundHandler.TryReceive(TimeSpan.FromSeconds(10), out msg, out reply);
                reply.Reply(msgReply, TimeSpan.FromSeconds(10));
                manualEvent.Set();
            });
            //Executing the step
            step.Execute(context);

            manualEvent.WaitOne(10000);

            Assert.IsNotNull(msg, "Message instance was not received");
            string expectedRequest = ReadRequestFileContent(step.RequestPath);
            string actualRequest = GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8, false);
            Assert.AreEqual(expectedRequest, actualRequest, 
                "Message contents of received message is different");
                        
            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))), Times.AtLeastOnce(), "The LogData message was not called");
            
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestRequest.xml")]
        [DeploymentItem(@"TestData\CustomFault.xml")]
        public void TestSendSmallMessage_XML_FaultResponse()
        {
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockSolicitResponseStep step = new MockSolicitResponseStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.RequestPath = "TestRequest.xml";
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            //Validating the test step
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            Message msg = null;
            IInboundReply reply;
            //Creating the reply message
            //Message msgReply = GeneralTestHelper.CreateMessageWithBase64EncodedBody(
            //    ReadRequestFileContent("CustomFault.xml"), Encoding.UTF8);
            Message msgReply = Message.CreateMessage(
                MessageVersion.Default,
                MessageFault.CreateFault(
                    new FaultCode("Custom"),
                    "wanna fail"),
                    "Test action");
                

            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                //Now we read the message in the inbound handler                
                inboundHandler.TryReceive(TimeSpan.FromSeconds(10), out msg, out reply);
                reply.Reply(msgReply, TimeSpan.FromSeconds(10));
                manualEvent.Set();
            });
            //Executing the step
            step.Execute(context);

            manualEvent.WaitOne(10000);

            Assert.IsNotNull(msg, "Message instance was not received");
            string expectedRequest = ReadRequestFileContent(step.RequestPath);
            string actualRequest = GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8, false);
            Assert.AreEqual(expectedRequest, actualRequest,
                "Message contents of received message is different");

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))), Times.AtLeastOnce(), "The LogData message was not called");



        }

        internal static Mock<ILogger> CreateLoggerMock()
        {
            Mock<ILogger> loggerMock = new Mock<ILogger>();
            
            loggerMock.Setup(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))))
                .Verifiable();

            return loggerMock;
        }

        internal static string ReadRequestFileContent(string path)
        {
            return Encoding.UTF8.GetString(File.ReadAllBytes(path));
        }
    }
}
