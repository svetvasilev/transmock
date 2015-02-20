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
        public void TestOneWay_XML()
        {
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule("USERS", PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite, 
               System.Security.AccessControl.AccessControlType.Allow));
            ////We first spin a pipe server to make sure that the send port will be able to connect
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
                    byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                    pipeClient.Connect(10000);
                    pipeClient.Write(xmlBytes, 0, xmlBytes.Count());

                    pipeClient.WriteByte(0x00);//writing the EOF byte
                    pipeClient.Flush();

                    pipeClient.WaitForPipeDrain();
                }
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedXml = Encoding.UTF8.GetString(testHelper.memStream.ToArray(), 0, (int)testHelper.memStream.Length);

                Assert.AreEqual(xml, receivedXml, "Contents of the received message is different");
            }            
        }

        void pipeServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            
        }

        [TestMethod]
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
                    byte[] flatBytes = Encoding.UTF8.GetBytes(ffContent);

                    pipeClient.Connect(10000);
                    pipeClient.Write(flatBytes, 0, flatBytes.Count());
                    pipeClient.Flush();

                    pipeClient.WriteByte(0x00);//writing the EOF byte
                    pipeClient.Flush();

                    pipeClient.WaitForPipeDrain();
                }
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedMsg = Encoding.UTF8.GetString(testHelper.memStream.ToArray(), 0, (int)testHelper.memStream.Length);

                Assert.AreEqual(ffContent, receivedMsg, "Contents of the received message is different");
            }
        }

        [TestMethod]
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
                    byte[] flatBytes = Encoding.ASCII.GetBytes(ffContent);

                    pipeClient.Connect(10000);
                    pipeClient.Write(flatBytes, 0, flatBytes.Count());
                    pipeClient.Flush();

                    pipeClient.WriteByte(0x00);//writing the EOF byte
                    pipeClient.Flush();

                    pipeClient.WaitForPipeDrain();
                }
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedMsg = Encoding.ASCII.GetString(testHelper.memStream.ToArray(), 0, (int)testHelper.memStream.Length);

                Assert.AreEqual(ffContent, receivedMsg, "Contents of the received message is different");
            }
        }

        [TestMethod]
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
                    byte[] flatBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ffContent);

                    pipeClient.Connect(10000);
                    pipeClient.Write(flatBytes, 0, flatBytes.Count());
                    pipeClient.Flush();

                    pipeClient.WriteByte(0x00);//writing the EOF byte
                    pipeClient.Flush();

                    pipeClient.WaitForPipeDrain();
                }
                //Here we wait for the event to be signalled
                testHelper.syncEvent.WaitOne(60000);
                //The event was signalled, we get the message stirng from the outBuffer
                string receivedMsg = Encoding.GetEncoding("ISO-8859-1").GetString(testHelper.memStream.ToArray(), 0, (int)testHelper.memStream.Length);

                Assert.AreEqual(ffContent, receivedMsg, "Contents of the received message is different");
            }
        }

        [TestMethod]
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

                pipeServer.BeginWaitForConnection(cb => testHelper.ClientConnectedSyncronous(cb), testHelper);
                //Here we spin the pipe client that will send the message to BizTalk
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                    "TwoWayReceive", PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                    pipeClient.Connect(10000);
                    pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                    pipeClient.Flush();

                    pipeClient.WriteByte(0x00);//writing the EOF byte
                    pipeClient.Flush();

                    pipeClient.WaitForPipeDrain();

                    //Here we wait for the event to be signalled
                    testHelper.syncEvent.WaitOne(10000);
                    //The event was signalled, we get the message stirng from the outBuffer
                    string receivedXml = Encoding.UTF8.GetString(testHelper.memStream.ToArray(), 0, (int)testHelper.memStream.Length);

                    Assert.AreEqual(xml, receivedXml, "Contents of received message is different");
                    //Here we read from the pipeClient the response message
                    byte[] responseBytes = new byte[256];
                    int responseByteCount = pipeClient.Read(responseBytes, 0, responseBytes.Length);

                    string receivedResponseXml = GeneralTestHelper.GetMessageFromArray(responseBytes,
                        responseByteCount, Encoding.UTF8);

                    Assert.AreEqual(responseXml, receivedResponseXml, "Contents of the response message is different");
                }
            }
        }
    }
}
