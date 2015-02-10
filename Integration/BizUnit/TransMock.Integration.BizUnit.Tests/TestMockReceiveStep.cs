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
    public class TestMockReceiveStep
    {
        public TestMockReceiveStep()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        MockAdapterConnectionUri connectionUri;
        MockAdapter adapter;
        MockAdapterOutboundHandler outboundHandler;

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
            outboundHandler = new MockAdapterOutboundHandler(connection, null);
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup() 
        {
            //give some time for the pipe to clean
            System.Threading.Thread.Sleep(100);
        }
        
        #endregion

        [TestMethod]
        public void TestValidateMethod_ValidStep()
        {
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);
            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            //Colling Validate in order to start the 
            step.Validate(context);
            step = null;
        }

        [TestMethod]
        public void TestReceiveSmallMessage_XML()
        {            
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();
            
            Context context = new Context(loggerMock.Object);

            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            //Colling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });
                        
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);            

            outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
            
            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);            

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))), 
                Times.AtLeastOnce(), "The LogData message was not called");
            
        }

        [TestMethod]
        public void TestReceiveSmallMessage_FlatFile()
        {
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            //Colling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

            Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(ffContent, Encoding.UTF8);

            outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));

            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);

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
    }
}
