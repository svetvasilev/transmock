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
    /// This test suite requires the BizTalk application MockAdapter.Test to be installed and up and running
    /// on a local BizTalk server instance. Please import the bindings included in the project to create the application
    /// and make sure it is started prior to executing the tests.
    /// </summary>
    [TestClass]    
    public class TestWithBizTalk
    {
        [TestMethod]
        [TestCategory("BizTalk Tests")]
        public void TestOneWay_XML()
        {
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(
                new PipeAccessRule(
                    "USERS", 
                    PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite, 
                    System.Security.AccessControl.AccessControlType.Allow));
            
            // We first spin a pipe server to make sure that the send port will be able to connect
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                "OneWaySend", PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                1024, 1024, ps))            
            {   
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";                           

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);
                
                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);
                //Here we spin the pipe client that will send the message to BizTalk
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                    "OneWayReceive", PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    var mockMessage = new MockMessage();
                    mockMessage.Body = xml;

                    using (var memStream = new MemoryStream())
                    {
                        var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                        formatter.Serialize(memStream, mockMessage);

                        pipeClient.Connect(10000);
                        pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                        pipeClient.WriteByte(0x00);
                        pipeClient.WaitForPipeDrain();
                    }
                    
                }
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                var receivedMsg = TestUtils.ConvertToMockMessage(testHelper.memStream);

                Assert.AreEqual(xml, receivedMsg.Body, "Contents of the received message is different");
                Assert.IsTrue(receivedMsg.Properties.Count > 1, "Received message does not contain properties");
                Assert.IsTrue(receivedMsg.Properties.ContainsKey(
                    "http://schemas.microsoft.com/BizTalk/2003/system-properties#BizTalkMessageID"),
                    "Received message does not contain MessageID property");
                
            }            
        }

        [TestMethod]
        [TestCategory("BizTalk Tests")]
        public void TestOneWay_FlatFile()
        {
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("USERS", PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow));
            //We first spin a pipe server to make sure that the send port will be able to connect
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                "OneWaySend", PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                1024, 1024, ps))
            {
                string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";                

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);
                //Here we spin the pipe client that will send the message to BizTalk
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                    "OneWayReceive", PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    var mockMessage = new MockMessage();
                    mockMessage.Body = ffContent;

                    using (var memStream = new MemoryStream())
                    {
                        var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                        formatter.Serialize(memStream, mockMessage);

                        pipeClient.Connect(10000);
                        pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                        pipeClient.WriteByte(0x00);
                        pipeClient.WaitForPipeDrain();
                    }
                }
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                var receivedMsg = TestUtils.ConvertToMockMessage(testHelper.memStream);

                Assert.AreEqual(ffContent, receivedMsg.Body, "Contents of the received message is different");
            }
        }

        [TestMethod]
        [TestCategory("BizTalk Tests")]
        public void TestOneWay_FlatFile_ASCII()
        {
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("USERS", PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow));
            //We first spin a pipe server to make sure that the send port will be able to connect
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                "OneWaySend", PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                1024, 1024, ps))
            {
                string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);
                //Here we spin the pipe client that will send the message to BizTalk
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                    "OneWayReceive", PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    var mockMessage = new MockMessage(Encoding.ASCII);
                    mockMessage.Body = ffContent;

                    using (var memStream = new MemoryStream())
                    {
                        var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                        formatter.Serialize(memStream, mockMessage);

                        pipeClient.Connect(10000);
                        pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                        pipeClient.WriteByte(0x00);
                        pipeClient.WaitForPipeDrain();
                    }
                }
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                var receivedMsg = TestUtils.ConvertToMockMessage(testHelper.memStream, Encoding.ASCII);

                Assert.AreEqual(ffContent, receivedMsg.Body, "Contents of the received message is different");
            }
        }

        [TestMethod]
        [TestCategory("BizTalk Tests")]
        public void TestOneWay_FlatFile_ISO88591()
        {
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("USERS", PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow));
            //We first spin a pipe server to make sure that the send port will be able to connect
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                "OneWaySend", PipeDirection.InOut, 1,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous,
                1024, 1024, ps))
            {
                string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer);

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnected(cb), testHelper);
                //Here we spin the pipe client that will send the message to BizTalk
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                    "OneWayReceive", PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    var mockMessage = new MockMessage(Encoding.GetEncoding("ISO-8859-1"));
                    mockMessage.Body = ffContent;

                    using (var memStream = new MemoryStream())
                    {
                        var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                        formatter.Serialize(memStream, mockMessage);

                        pipeClient.Connect(10000);
                        pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                        pipeClient.WriteByte(0x00);
                        pipeClient.WaitForPipeDrain();
                    }
                }
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedMsg = Encoding.GetEncoding("ISO-8859-1").GetString(testHelper.memStream.ToArray(), 0, (int)testHelper.memStream.Length);

                Assert.AreEqual(ffContent, receivedMsg, "Contents of the received message is different");
            }
        }

        [TestMethod]
        [TestCategory("BizTalk Tests")]
        public void TestTwoWay_XML()
        {
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("USERS", PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite,
                System.Security.AccessControl.AccessControlType.Allow));
            //We first spin a pipe server to make sure that the send port will be able to connect
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                "TwoWaySend", PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                1024, 1024, ps))
            {
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";
                string responseXml = "<SomeTestMessageResponse><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessageResponse>";

                OutboundTestHelper testHelper = new OutboundTestHelper(pipeServer, responseXml);

                pipeServer.BeginWaitForConnection(cb => testHelper
                    .ClientConnectedSyncronous(cb, ctx => TestMockAdapterOutboundHandler.SendResponse(ctx)), testHelper);

                System.Threading.Thread.Sleep(100);
                //Here we spin the pipe client that will send the message to BizTalk
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                    "TwoWayReceive", PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    var mockMessage = new MockMessage();
                    mockMessage.Body = xml;

                    using (var memStream = new MemoryStream())
                    {
                        var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                        formatter.Serialize(memStream, mockMessage);

                        pipeClient.Connect(10000);
                        pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                        pipeClient.WriteByte(0x00);
                        pipeClient.WaitForPipeDrain();
                    }

                    //Here we wait for the event to be signalled
                    bool waitExpired = testHelper.syncEvent.WaitOne(10000);

                    Assert.IsTrue(waitExpired, "The waiting time for the response has expired prior to receiving the response");

                    //The event was signalled, we get the message stirng from the outBuffer
                    string receivedXml = Encoding.UTF8.GetString(testHelper.memStream.ToArray(), 0, (int)testHelper.memStream.Length);

                    Assert.AreEqual(xml, receivedXml, "Contents of received message is different");
                    //Here we read from the pipeClient the response message
                    byte[] responseBytes = new byte[256];

                    using (MemoryStream memStream = new MemoryStream(256))
                    {
                        int byteCountRead = 0;
                        bool eofReached = false;
                        while (!eofReached)
                        {
                            byteCountRead = pipeClient.Read(responseBytes, 0, responseBytes.Length);

                            if (byteCountRead > 2)
                            {
                                eofReached = (responseBytes[byteCountRead - 1] == 0x0 &&
                                    responseBytes[byteCountRead - 2] != 0x0 &&
                                    responseBytes[byteCountRead - 3] != 0x0);
                            }
                            else if (byteCountRead > 1 && !eofReached)
                            {
                                eofReached = (responseBytes[byteCountRead - 1] == 0x0 &&
                                    responseBytes[byteCountRead - 2] == 0x0);
                            }
                            else if (byteCountRead == 1)
                            {
                                eofReached = responseBytes[byteCountRead - 1] == 0x0;
                            }

                            memStream.Write(responseBytes, 0,
                                eofReached ? byteCountRead - 1 : byteCountRead);
                        }                        

                        string receivedResponseXml = GeneralTestHelper.GetMessageFromArray(
                            memStream.ToArray(),
                            (int)memStream.Length, 
                            Encoding.UTF8);

                        Assert.AreEqual(responseXml, receivedResponseXml, "Contents of the response message is different");
                    }
                }
            }
        }
    }
}
