/***************************************
//   Copyright 2020 - Svetoslav Vasilev

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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransMock.Communication.NamedPipes;

namespace TransMock.Communication.NamedPipes.Tests
{
    [TestClass]
    public class TestStreamingNamedPipeServerAsync
    {
        private IStreamingServerAsync pipeServer;
        private ManualResetEventSlim syncEvent;

        [TestInitialize]
        public async void TestInitialize()
        {
            syncEvent = new ManualResetEventSlim(false);
            pipeServer = new StreamingNamedPipeServerAsync("TestPipeServer");
            await pipeServer.StartAsync();
        }

        [TestCleanup]
        public async void TestCleanup()
        {
            await pipeServer.StopAsync();
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

                receivedXml = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }
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
                receivedXml = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage(Encoding.Unicode);
                mockMessage.Body = xml;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }

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
                receivedContent = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = ffContent;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }

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
                receivedContent = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage(Encoding.ASCII);
                mockMessage.Body = ffContent;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }

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
                receivedContent = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage()
                {
                    Encoding = Encoding.Unicode
                };
                mockMessage.Body = ffContent;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedContent, "Message was not received by the server");
            Assert.AreEqual(ffContent, receivedContent, "Contents of received message is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_MockMessage_XML()
        {
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";
            string receivedXml = null;

            pipeServer.ReadCompleted += (o, readArgs) => {

                receivedXml = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }
                
                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedXml, "Message was not received by the server");
            Assert.AreEqual(xml, receivedXml, "Contents of received message is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_MockMessage_XML_Unicode()
        {
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";
            string receivedXml = null;

            pipeServer.ReadCompleted += (o, readArgs) =>
            {
                receivedXml = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage()
                {
                    Encoding = Encoding.Unicode
                };
                mockMessage.Body = xml;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedXml, "Message was not received by the server");
            Assert.AreEqual(xml, receivedXml, "Contents of received message is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_MockMessage_XML_Unicode_Base64()
        {
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";
            string receivedXml = null;

            pipeServer.ReadCompleted += (o, readArgs) =>
            {
                receivedXml = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage()
                {
                    Encoding = Encoding.Unicode
                };
                mockMessage.Body = xml;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            //string xmlBase64 = Convert.ToBase64String(
            //    Encoding.Unicode.GetBytes(xml));

            Assert.IsNotNull(receivedXml, "Message was not received by the server");
            Assert.AreEqual(xml, receivedXml, "Contents of received message is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_MockMessage_XML_ASCII_Base64()
        {
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";
            string receivedXml = null;

            pipeServer.ReadCompleted += (o, readArgs) =>
            {
                receivedXml = readArgs.Message.BodyBase64;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage()
                {
                    Encoding = Encoding.ASCII
                };
                mockMessage.Body = xml;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            string xmlBase64 = Convert.ToBase64String(
                Encoding.ASCII.GetBytes(xml));

            Assert.IsNotNull(receivedXml, "Message was not received by the server");
            Assert.AreEqual(xmlBase64, receivedXml, "Contents of received message is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        public void TestOneWayReceive_MockMessage_XML_FromFile()
        {
            string xml = File.ReadAllText("StartMessage.xml");
            string receivedXml = null;

            pipeServer.ReadCompleted += (o, readArgs) => {

                receivedXml = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage("StartMessage.xml", Encoding.UTF8);               

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedXml, "Message was not received by the server");
            Assert.AreEqual(xml, receivedXml.Trim('\u0000'), "Contents of received message is different");
        }


        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_MockMessage_FlatFile()
        {
            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";
            string receivedContent = null;

            pipeServer.ReadCompleted += (o, readArgs) =>
            {
                receivedContent = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = ffContent;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.Flush();

                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedContent, "Message was not received by the server");
            Assert.AreEqual(ffContent, receivedContent, "Contents of received message is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_MockMessage_FlatFile_ASCII()
        {
            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";
            MockMessage receivedMessage = null;

            pipeServer.ReadCompleted += (o, readArgs) =>
            {
                receivedMessage = readArgs.Message;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage(Encoding.ASCII);
                mockMessage.Body = ffContent;
                mockMessage.Properties.Add("SomeProperty1", "TestVal1");
                mockMessage.Properties.Add("SomeProperty2", "TestVal2");
                mockMessage.Properties.Add("SomeProperty3", "TestVal3");

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedMessage, "Message was not received by the server");
            Assert.AreEqual(ffContent, receivedMessage.Body, "Contents of received message is different");
            Assert.AreEqual(3, receivedMessage.Properties.Count, "Number of properties in received message is different");
            Assert.AreEqual("TestVal1", 
                receivedMessage.Properties["SomeProperty1"], "Value of property 1 is different");
            Assert.AreEqual("TestVal2",
                receivedMessage.Properties["SomeProperty2"], "Value of property 1 is different");
            Assert.AreEqual("TestVal3",
                receivedMessage.Properties["SomeProperty3"], "Value of property 1 is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_MockMessage_FlatFile_Unicode()
        {
            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";
            string receivedContent = null;

            pipeServer.ReadCompleted += (o, readArgs) =>
            {
                receivedContent = readArgs.Message.Body;

                syncEvent.Set();
            };

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                "TestPipeServer", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage(Encoding.Unicode);
                mockMessage.Body = ffContent;

                using (MemoryStream msgStream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    pipeClient.Connect(10000);

                    formatter.Serialize(msgStream, mockMessage);

                    WriteMockMessage(pipeClient, msgStream);

                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            syncEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsNotNull(receivedContent, "Message was not received by the server");
            Assert.AreEqual(ffContent, receivedContent, "Contents of received message is different");
        }

        private void WriteMockMessage(NamedPipeClientStream pipeClient, MemoryStream msgStream)
        {
            pipeClient.Write(msgStream.ToArray(), 0, (int)msgStream.Length);
            // Write EndOfMessage
            pipeClient.Write(
                NamedPipeMessageUtils.EndOfMessage,
                0,
                NamedPipeMessageUtils.EndOfMessage.Length);
        }
    }
}
