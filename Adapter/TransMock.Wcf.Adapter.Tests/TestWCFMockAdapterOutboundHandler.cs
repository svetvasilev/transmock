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

using TransMock.Communication.NamedPipes;
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
        [TestCategory("One Way Tests")]
        public void TestSendOneWay_XML()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1, 
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);
                
                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);

                outboundHandler.Execute(msg, new TimeSpan(0,0,10));
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                var mockMessage = ConvertToMockMessage(testHelper.memStream);

                Assert.AreEqual(xml, mockMessage.Body, "Contents of received message is different");
            }
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestSendOneWay_XML_Unicode()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                adapter.Encoding = "Unicode";
                
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.Unicode);

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);

                outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                var mockMessage = ConvertToMockMessage(testHelper.memStream);

                Assert.AreEqual(xml, mockMessage.Body, "Contents of received message is different");
            }
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestSendOneWay_FlatFile()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {                
                string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(ffContent, Encoding.UTF8);               

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);

                outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                var mockMessage = ConvertToMockMessage(testHelper.memStream);

                Assert.AreEqual(ffContent, mockMessage.Body, "Contents of received message is different");
            }
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestSendOneWay_FlatFile_ASCII()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
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
                var mockMessage = ConvertToMockMessage(testHelper.memStream);

                Assert.AreEqual(ffContent, mockMessage.Body, "Contents of received message is different");
            }
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestSendOneWay_FlatFile_ISO88591()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
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
                var mockMessage = ConvertToMockMessage(testHelper.memStream);

                Assert.AreEqual(ffContent, mockMessage.Body, "Contents of received message is different");
            }
        }
        
        [TestMethod]
        [TestCategory("Two Way Tests")]
        public void TestSendTwoWay_XML()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {   
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);
                //Setting up the BTS.IsSolicitResponse property
                msg.Properties.Add("http://schemas.microsoft.com/BizTalk/2003/system-properties#IsSolicitResponse", true);

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);
                //We set the response message content
                testHelper.responseXml = "<SomeTestMessageResponse><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessageResponse>";

                pipeServer.BeginWaitForConnection(cb => testHelper
                    .ClientConnectedSyncronous(cb, ctx => SendResponse(ctx)), testHelper);

                Message responseMsg = outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));

                var mockMessage = ConvertToMockMessage(testHelper.memStream);

                Assert.AreEqual(mockMessage.Body, xml, "Contents of the request message is different");
                Assert.AreEqual(testHelper.responseXml, 
                    GeneralTestHelper.GetBodyAsString(responseMsg, Encoding.UTF8), 
                    "Contents of the response message is different");
            }
        }

        [TestMethod]
        [TestCategory("Two Way Tests")]
        [DeploymentItem(@"TestData\SmallMessage.xml")]
        public void TestSendTwoWay_XML_SmallMessage()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(
                    File.ReadAllBytes("SmallMessage.xml"));
                //Setting up the BTS.IsSolicitResponse property
                msg.Properties.Add("http://schemas.microsoft.com/BizTalk/2003/system-properties#IsSolicitResponse", true);

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);
                //We set the response message content
                testHelper.responsePath = "SmallMessage.xml";

                pipeServer.BeginWaitForConnection(cb => testHelper
                    .ClientConnectedSyncronous(cb, ctx => SendResponse(ctx)), testHelper);

                Message responseMsg = outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));
                //Calculating the hashes of the messages and the file
                byte[] responseMessageBytes = GeneralTestHelper.GetBodyAsBytes(responseMsg);

                var mockMessage = ConvertToMockMessage(testHelper.memStream);

                string receivedMessageHash = GeneralTestHelper.CalculateBytesHash(
                    Convert.FromBase64String(mockMessage.BodyBase64));
                string responseMessageHash = GeneralTestHelper.CalculateBytesHash(responseMessageBytes);
                string fileHash = GeneralTestHelper.CalculateFileHash("SmallMessage.xml");

                Assert.AreEqual(fileHash, 
                    receivedMessageHash, 
                    "Contents of the request message is different");
                
                Assert.AreEqual(fileHash,
                    responseMessageHash, 
                    "Contents of the response message is different");
            }
        }

        [TestMethod]
        [TestCategory("Two Way Tests")]
        [DeploymentItem(@"TestData\MediumMessage.xml")]
        public void TestSendTwoWay_XML_MediumMessage()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Message msg = GeneralTestHelper
                    .CreateMessageWithBase64EncodedBody(
                        File.ReadAllBytes(
                            testContextInstance.TestDeploymentDir + @"\MediumMessage.xml"));
                //Setting up the BTS.IsSolicitResponse property
                msg.Properties.Add("http://schemas.microsoft.com/BizTalk/2003/system-properties#IsSolicitResponse", true);

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);
                //We set the response message content
                testHelper.responsePath = "MediumMessage.xml";

                pipeServer.BeginWaitForConnection(cb => testHelper
                    .ClientConnectedSyncronous(cb, ctx => SendResponse(ctx)), testHelper);

                Message responseMsg = outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));                              
                //Calculating the hashes of the messages and the file
                byte[] responseMessageBytes = GeneralTestHelper.GetBodyAsBytes(responseMsg);

                var mockMessage = ConvertToMockMessage(testHelper.memStream);

                string receivedMessageHash = GeneralTestHelper.CalculateBytesHash(
                    Convert.FromBase64String(mockMessage.BodyBase64));
                string responseMessageHash = GeneralTestHelper.CalculateBytesHash(responseMessageBytes);
                string fileHash = GeneralTestHelper.CalculateFileHash("MediumMessage.xml");
                //Validating the results
                Assert.AreEqual(fileHash, receivedMessageHash, "Contents of the request message is different");

                Assert.AreEqual(fileHash,
                    responseMessageHash,
                    "Contents of the response message is different");
            }
        }

        [TestMethod]
        [TestCategory("One Way Tests with Properties")]
        public void TestSendOneWay_XML_WithProperties()
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);

                // Adding test properties
                AddPromotedProperty(msg, "HTTP.Content-Type", "text/json");                

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);

                outboundHandler.Execute(msg, new TimeSpan(0, 0, 10));
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                var mockMessage = ConvertToMockMessage(testHelper.memStream);

                Assert.AreEqual(xml, mockMessage.Body, "Contents of received message is different");
                Assert.IsTrue(mockMessage.Properties.Count == 1, "Number of properties received from outbound adapter is wrong");
                Assert.AreEqual("text/json", mockMessage.Properties["HTTP.Content-Type"], "The promoted property is not as expected");
                
            }
        }

        private void AddPromotedProperty(Message msg, string name, string value)
        {
            var propertiesList = new List<KeyValuePair<XmlQualifiedName, object>>(3);

            propertiesList.Add(
                new KeyValuePair<XmlQualifiedName, object>(
                    new XmlQualifiedName(name, "http://transmock.tests"),
                        value)
                );

            msg.Properties.Add(
                "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote",
                propertiesList);
        }

        public static void SendResponse(OutboundTestHelper testHelper)
        {
            // Sending a response
            if (!string.IsNullOrEmpty(testHelper.responseXml))
            {
                var responseMessage = new MockMessage()
                {
                    Body = testHelper.responseXml
                };

                using (var msgStream = new MemoryStream(256))
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(msgStream, responseMessage);
                    msgStream.Seek(0, SeekOrigin.Begin);

                    //We write the response content back and flush it down the drain.
                    testHelper.pipeServer.Write(msgStream.ToArray(), 0, (int)msgStream.Length);
                }

            }
            else if (!string.IsNullOrEmpty(testHelper.responsePath))
            {
                var responseMessage = new MockMessage(testHelper.responsePath, testHelper.responseEncoding);
                using (var msgStream = new MemoryStream(256))
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(msgStream, responseMessage);
                    msgStream.Seek(0, SeekOrigin.Begin);

                    int byteCountRead = 0;
                    byte[] outBuffer = new byte[512];
                    
                    //Streaming respoonse from file
                    while ((byteCountRead = msgStream.Read(outBuffer, 0, outBuffer.Length)) > 0)
                    {
                        testHelper.pipeServer.Write(outBuffer, 0, byteCountRead);
                    }
                }
            }
            else
                throw new InvalidOperationException("There was no response content defined");

            //Write the EOF bytes
            testHelper.pipeServer.WriteByte(0x00);
            testHelper.pipeServer.Flush();

            testHelper.pipeServer.WaitForPipeDrain();
        }
        
        private static MockMessage ConvertToMockMessage(Stream messageStream)
        {
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            messageStream.Seek(0, SeekOrigin.Begin);

            return (MockMessage)formatter.Deserialize(messageStream);
        }
    }    
}
