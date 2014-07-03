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
using System.IO;
using System.IO.Pipes;
using System.ServiceModel.Channels;
using System.Xml;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransMock.TestUtils;

namespace TransMock.Wcf.Adapter.Tests
{
    /// <summary>
    /// Summary description for TestWCFMockAdapterOutboundHandler
    /// </summary>
    [TestClass]
    public class TestMockAdapterOutboundHandler
    {
        MockAdapterConnectionUri connectionUri;
        MockAdapter adapter;
        MockAdapterOutboundHandler outboundHandler;

        public TestMockAdapterOutboundHandler()
        {
            //
            // TODO: Add constructor logic here
            //
        }

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

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            
        }
        #endregion

        [TestMethod]
        public void TestSendOneWay_XML()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1, 
                PipeTransmissionMode.Message, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);
                
                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);

                outboundHandler.Execute(msg, new TimeSpan(0,0,10));
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedResponseXml = GeneralTestHelper.GetMessageFromArray(testHelper.outBuffer, testHelper.bytesReadCount, Encoding.UTF8);

                Assert.AreEqual(xml, receivedResponseXml, "Contents of received message is different");
            }
        }

        [TestMethod]
        public void TestSendOneWay_XML_Unicode()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous))
            {
                adapter.Encoding = "Unicode";
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.Unicode);

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);

                outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedResponseXml = GeneralTestHelper.GetMessageFromArray(testHelper.outBuffer, testHelper.bytesReadCount, Encoding.Unicode);

                Assert.AreEqual(xml, receivedResponseXml, "Contents of received message is different");
            }
        }

        [TestMethod]
        public void TestSendOneWay_FlatFile()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous))
            {                
                string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(ffContent, Encoding.UTF8);               

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);

                outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedResponse = GeneralTestHelper.GetMessageFromArray(testHelper.outBuffer, testHelper.bytesReadCount, Encoding.UTF8);
                //Need to decode it once more from base64 to normal string
                //receivedResponse = GeneralTestHelper.GetMessageFromArray(testHelper.outBuffer, testHelper.bytesReadCount, Encoding.UTF8);

                Assert.AreEqual(ffContent, receivedResponse, "Contents of received message is different");
            }
        }

        [TestMethod]
        public void TestSendOneWay_FlatFile_ASCII()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous))
            {
                adapter.Encoding = "ASCII";
                string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(ffContent, Encoding.ASCII);

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);

                outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedResponse = GeneralTestHelper.GetMessageFromArray(testHelper.outBuffer, testHelper.bytesReadCount, Encoding.UTF8);
                //Need to decode it once more from base64 to normal string
                //receivedResponse = Encoding.ASCII.GetString(Convert.FromBase64String(receivedResponse));

                Assert.AreEqual(ffContent, receivedResponse, "Contents of received message is different");
            }
        }

        [TestMethod]
        public void TestSendOneWay_FlatFile_ISO88591()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous))
            {
                adapter.Encoding = "ISO-8859-1";
                string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(ffContent, Encoding.GetEncoding("ISO-8859-1"));

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);

                outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedResponse = GeneralTestHelper.GetMessageFromArray(testHelper.outBuffer, 
                    testHelper.bytesReadCount, Encoding.GetEncoding("ISO-8859-1"));
                //Need to decode it once more from base64 to normal string
                //receivedResponse = Encoding.GetEncoding("ISO-8859-1").GetString(Convert.FromBase64String(receivedResponse));

                Assert.AreEqual(ffContent, receivedResponse, "Contents of received message is different");
            }
        }

        [TestMethod]
        public void TestSendTwoWay_XML()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);
                //Setting up the BTS.CorrelationToken property
                msg.Properties.Add("http://schemas.microsoft.com/BizTalk/2003/system-properties#CorrelationToken", "TestCorrelationToke");

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);
                //We set the response message content
                testHelper.responseXml = "<SomeTestMessageResponse><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessageResponse>";

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnectedSyncronous(cb), testHelper);

                Message responseMsg = outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));
                //Here we wait for the event to be signalled
                //testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedXml = GeneralTestHelper.GetMessageFromArray(testHelper.outBuffer,
                    testHelper.bytesReadCount, Encoding.UTF8);

                Assert.AreEqual(receivedXml, xml, "Contents of the request message is different");
                Assert.AreEqual(testHelper.responseXml, 
                    GeneralTestHelper.GetBodyAsString(responseMsg, Encoding.UTF8), "Contents of the response message is different");
            }
        }        
    }

    
}
