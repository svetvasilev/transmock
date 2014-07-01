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

namespace TransMock.Integration.BizUnit.Tests
{
    /// <summary>
    /// Tests the MockReceiveStep class
    /// </summary>
    [TestClass]
    public class TestMockSendStep
    {
        public TestMockSendStep()
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
            connectionUri = new MockAdapterConnectionUri(new Uri("mock://localhost/TestEndpoint"));
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
        }
        //
        #endregion

        [TestMethod]
        public void TestValidateMethod_ValidStep()
        {
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);
            MockSendStep step = new MockSendStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.RequestPath = "TestRequest.xml";
            step.Encoding = "UTF-8";
            //Colling Validate in order to start the 
            step.Validate(context);
            step = null;
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestRequest.xml")]
        public void TestSendSmallMessage_XML()
        {            
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();
            
            Context context = new Context(loggerMock.Object);

            MockSendStep step = new MockSendStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.RequestPath = "TestRequest.xml";
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            //Validating the test step
            step.Validate(context);
            //Executing the step
            step.Execute(context);          
            
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(TimeSpan.FromSeconds(10), out msg, out reply);

            Assert.IsNotNull(msg, "Message instance was not returned");            
            Assert.AreEqual(ReadRequestFileContent(step.RequestPath),
                GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");
                        
            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))), Times.AtLeastOnce(), "The LogData message was not called");
            
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestRequest.txt")]
        public void TestSendSmallMessage_FlatFile()
        {
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockSendStep step = new MockSendStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.RequestPath = "TestRequest.txt";
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            //Validated the step
            step.Validate(context);
            //Executing the step
            step.Execute(context);

            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(TimeSpan.FromSeconds(10), out msg, out reply);

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(ReadRequestFileContent(step.RequestPath),
                GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");

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
            return System.IO.File.ReadAllText(path, Encoding.UTF8);
        }
    }
}
