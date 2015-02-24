/***************************************
//   Copyright 2015 - Svetoslav Vasilev

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
using System.IO;
using System.IO.Pipes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransMock.Communication.NamedPipes;

namespace TransMock.Communication.NamedPipes.Tests
{
    [TestClass]
    public class TestStreamingNamedPipeServer
    {
        private IAsyncStreamingServer pipeServer;
        private ManualResetEventSlim syncEvent;

        [TestInitialize]
        public void TestInitialize()
        {
            syncEvent = new ManualResetEventSlim(false);
            pipeServer = new StreamingNamedPipeServer("TestPipeServer");
            pipeServer.Start();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            pipeServer.Stop();
            pipeServer = null;
            syncEvent = null;
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_XML()
        {
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";
            string receivedXml = null;

            pipeServer.ReadCompleted += (o, readArgs) => {
                using (StreamReader sr = new StreamReader(readArgs.MessageStream, Encoding.UTF8))
                {
                    receivedXml = sr.ReadToEnd();
                }

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.WaitForPipeDrain();
                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedXml, "Message was not received by the server");
            Assert.AreEqual(xml, receivedXml, "Contents of received message is different");

        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_XML_Unicode()
        {
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";
            string receivedXml = null;

            pipeServer.ReadCompleted += (o, readArgs) =>
            {
                using (StreamReader sr = new StreamReader(readArgs.MessageStream, Encoding.Unicode))
                {
                    receivedXml = sr.ReadToEnd();
                }

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                byte[] xmlBytes = Encoding.Unicode.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.WaitForPipeDrain();
                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedXml, "Message was not received by the server");
            Assert.AreEqual(xml, receivedXml, "Contents of received message is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_FlatFile()
        {
            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";
            string receivedContent = null;

            pipeServer.ReadCompleted += (o, readArgs) =>
            {
                using (StreamReader sr = new StreamReader(readArgs.MessageStream, Encoding.UTF8))
                {
                    receivedContent = sr.ReadToEnd();
                }

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                byte[] msgBytes = Encoding.UTF8.GetBytes(ffContent);

                pipeClient.Connect(10000);
                pipeClient.Write(msgBytes, 0, msgBytes.Count());
                pipeClient.WaitForPipeDrain();
                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedContent, "Message was not received by the server");
            Assert.AreEqual(ffContent, receivedContent, "Contents of received message is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_FlatFile_ASCII()
        {
            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";
            string receivedContent = null;

            pipeServer.ReadCompleted += (o, readArgs) =>
            {
                using (StreamReader sr = new StreamReader(readArgs.MessageStream, Encoding.ASCII))
                {
                    receivedContent = sr.ReadToEnd();
                }

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                byte[] msgBytes = Encoding.ASCII.GetBytes(ffContent);

                pipeClient.Connect(10000);
                pipeClient.Write(msgBytes, 0, msgBytes.Count());
                pipeClient.WaitForPipeDrain();
                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedContent, "Message was not received by the server");
            Assert.AreEqual(ffContent, receivedContent, "Contents of received message is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_FlatFile_Unicode()
        {
            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";
            string receivedContent = null;

            pipeServer.ReadCompleted += (o, readArgs) =>
            {
                using (StreamReader sr = new StreamReader(readArgs.MessageStream, Encoding.Unicode))
                {
                    receivedContent = sr.ReadToEnd();
                }

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                byte[] msgBytes = Encoding.Unicode.GetBytes(ffContent);

                pipeClient.Connect(10000);
                pipeClient.Write(msgBytes, 0, msgBytes.Count());
                pipeClient.WaitForPipeDrain();
                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedContent, "Message was not received by the server");
            Assert.AreEqual(ffContent, receivedContent, "Contents of received message is different");
        }
    }
}
