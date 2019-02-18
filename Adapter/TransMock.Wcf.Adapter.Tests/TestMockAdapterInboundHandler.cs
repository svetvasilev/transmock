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
using Microsoft.ServiceModel.Channels.Common;

using TransMock.Communication.NamedPipes;
using TransMock.Wcf.Adapter;
using TransMock.TestUtils;

namespace TransMock.Wcf.Adapter.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestMockAdapterInboundHandler
    {
        int endpointId = 0;
        public TestMockAdapterInboundHandler()
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

        MockAdapterConnectionUri connectionUri;
        MockAdapter adapter;
        MockAdapterInboundHandler inboundHandler;

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
            InitInboundHandler(
                string.Format("mock://localhost/TestEndpoint{0}", endpointId++), 
                null);
        }        
       
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup() 
        {
            if (inboundHandler != null)
               inboundHandler.StopListener(new TimeSpan(0, 0, 10));

            //inboundHandler.Dispose();
            inboundHandler = null;

            System.Threading.Thread.Sleep(100);//Give some time of the named pipe to cleanup itself.
        }
        
        #endregion

        #region One way tests
        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_XML()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();                   
                }

                pipeClient.Close();

            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");
            
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_XML_Unicode()
        {
            adapter.Encoding = "Unicode";
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage(Encoding.Unicode);
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.Unicode), "Message contents of received message is different");        
        }

        [TestMethod]
        public void TestOneWayReceive_XML_ISO88591()
        {
            adapter.Encoding = "ISO-8859-1";
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            //TODO: implement sending XML message to the inbound handler
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage(Encoding.GetEncoding("ISO-8859-1"));
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(
                xml, 
                GeneralTestHelper.GetBodyAsString(msg, Encoding.GetEncoding("ISO-8859-1")), 
                "Message contents of received message is different");
            
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        [DeploymentItem(@"..\TestData\SmallMessage.xml")]
        public void TestOneWayReceive_XML_SmallMessage()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            byte[] xmlBytes = new byte[512];
            int fileLength = (int)File.OpenRead("SmallMessage.xml").Length;

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                pipeClient.Connect(10000);

                int byteCountRead = 0;

                var mockMessage = new MockMessage(
                    "SmallMessage.xml",
                    Encoding.UTF8);

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(memStream, mockMessage);
                    memStream.Seek(0, SeekOrigin.Begin);
                    
                    
                    while ((byteCountRead = memStream.Read(xmlBytes, 0, xmlBytes.Length)) > 0)
                    {
                        pipeClient.Write(xmlBytes, 0, byteCountRead);
                    }

                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }                
            }

            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.IsNotNull(reply, "Reply instance was not returned");

            byte[] receivedMessageBytes = GeneralTestHelper.GetBodyAsBytes(msg);

            Assert.AreEqual(fileLength, receivedMessageBytes.Length, "The message length is wrong");

            string fileHash = GeneralTestHelper.CalculateFileHash("SmallMessage.xml");
            string messageHash = GeneralTestHelper.CalculateBytesHash(receivedMessageBytes);

            Assert.AreEqual(fileHash, messageHash,
                "Message contents of received message is different");            
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        [DeploymentItem(@"..\TestData\MediumMessage.xml")]
        public void TestOneWayReceive_XML_MediumMessage()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            byte[] xmlBytes = new byte[512];
            int fileLength = (int)File.OpenRead("MediumMessage.xml").Length;

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                pipeClient.Connect(10000);

                int byteCountRead = 0;

                var mockMessage = new MockMessage(
                    "MediumMessage.xml",
                    Encoding.UTF8);

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(memStream, mockMessage);
                    // rewinding the message stream
                    memStream.Seek(0, SeekOrigin.Begin);

                    while ((byteCountRead = memStream.Read(xmlBytes, 0, xmlBytes.Length)) > 0)
                    {
                        pipeClient.Write(xmlBytes, 0, byteCountRead);
                    }

                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }
            }

            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.IsNotNull(reply, "Reply instance was not returned");

            byte[] receivedMessageBytes = GeneralTestHelper.GetBodyAsBytes(msg);

            Assert.AreEqual(fileLength, receivedMessageBytes.Length, "The message length is wrong");

            string fileHash = GeneralTestHelper.CalculateFileHash("MediumMessage.xml");
            string messageHash = GeneralTestHelper.CalculateBytesHash(receivedMessageBytes);

            Assert.AreEqual(fileHash, messageHash,
                "Message contents of received message is different");
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_XML_TwoMessages()
        {
            //TODO: implement sending XML message to the inbound handler
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            //byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }

            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received first message is different");

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received second message is different");
        }        

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_FlatFile()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));
            
            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = ffContent;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(ffContent,
                GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), 
                "Message contents of received message is different");
            //Assert.AreEqual(string.Format("<FFContent>{0}</FFContent>", Convert.ToBase64String(ffBytes)),
            //    GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");        
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_FlatFile_Unicode()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage(Encoding.Unicode);
                mockMessage.Body = ffContent;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(ffContent,
                GeneralTestHelper.GetBodyAsString(msg, Encoding.Unicode), "Message contents of received message is different");        
        }

        [TestMethod]
        [TestCategory("One Way Tests")]
        public void TestOneWayReceive_FlatFile_ISO88591()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));
                
            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage(Encoding.GetEncoding("ISO-8859-1"));
                mockMessage.Body = ffContent;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(ffContent,
                GeneralTestHelper.GetBodyAsString(msg, Encoding.GetEncoding("ISO-8859-1")), "Message contents of received message is different");
        
        } 
        #endregion

        #region Static property promotion tests
        [TestMethod]
        [TestCategory("One Way Tests with Properties")]
        public void TestOneWayReceive_XML_PromoteFileAdapterProperties()
        {
            //Initializing the inbound handler again and passing the desired property
            InitInboundHandler(
                string.Format(
                    "mock://localhost/TestEndpoint{0}",
                    endpointId),
                @"FILE.ReceivedFileName=C:\Test\In\File1.xml");            

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            //TODO: implement sending XML message to the inbound handler
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");

            //Verify the promoted properties
            var promotedProperties = msg.Properties
                .Where(p => p.Key == "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote")
                .Single();
            Assert.IsNotNull(promotedProperties, "The promoted properties list is not present");

            var propertiesList = promotedProperties.Value as List<KeyValuePair<XmlQualifiedName, object>>;

            Assert.AreEqual(propertiesList.Count, 1, "The element count in the promoted properties list differ");
                
            MessagePropertyValidator
                .ValidatePromotedProperty(propertiesList[0],
                "http://schemas.microsoft.com/BizTalk/2003/file-properties",
                "ReceivedFileName",
                @"C:\Test\In\File1.xml");
        }

        [TestMethod]
        [TestCategory("One Way Tests with Properties")]
        public void TestOneWayReceive_XML_PromoteFTPAdapterProperties()
        {
            //Initializing the inbound handler again and passing the desired property
            InitInboundHandler("mock://localhost/TestEndpoint", @"FTP.ReceivedFileName=File1.xml");
            
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            //TODO: implement sending XML message to the inbound handler
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");

            //Verify the promoted properties
            var promotedProperties = msg.Properties
                .Where(p => p.Key == "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote")
                .Single();
            Assert.IsNotNull(promotedProperties, "The promoted properties list is not present");

            var propertiesList = promotedProperties.Value as List<KeyValuePair<XmlQualifiedName, object>>;

            Assert.AreEqual(propertiesList.Count, 1, "The element count in the promoted properties list differ");

            MessagePropertyValidator
                .ValidatePromotedProperty(propertiesList[0],
                    "http://schemas.microsoft.com/BizTalk/2003/ftp-properties",
                    "ReceivedFileName",
                    "File1.xml");        
        }

        [TestMethod]
        [TestCategory("One Way Tests with Properties")]
        public void TestOneWayReceive_XML_PromotePOP3AdapterProperties()
        {
            const string adapterNamespace = "http://schemas.microsoft.com/BizTalk/2003/pop3-properties";

            string[] propertyArray = new string[]{
                "POP3.From=someone@test.com",
                "POP3.Subject=Test mail",
                "POP3.To=receiver@test.com",
                "POP3.CC=interested@test.com",
                "POP3.ReplyTo=receiver2@test.com"
            };

            //Initializing the inbound handler again and passing the desired property
            string propertiesToPromote = string.Join(";", propertyArray);
            InitInboundHandler(
                string.Format(
                    "mock://localhost/TestEndpoint{0}",
                    endpointId),
                propertiesToPromote);

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");

            //Verify the promoted properties
            var promotedProperties = msg.Properties
                .Where(p => p.Key == "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote")
                .Single();
            Assert.IsNotNull(promotedProperties, "The promoted properties list is not present");

            var propertiesList = promotedProperties.Value as List<KeyValuePair<XmlQualifiedName, object>>;

            Assert.AreEqual(5, propertiesList.Count, "The element count in the promoted properties list differ");

            for (int i = 0; i < propertiesList.Count; i++)
            {
                string[] propertyDetails = propertyArray[i].Split('=');

                MessagePropertyValidator.
                    ValidatePromotedProperty(propertiesList[i],
                        adapterNamespace,
                        propertyDetails[0].Replace("POP3.", string.Empty),
                        propertyDetails[1]);
            }        
        }

        [TestMethod]
        [TestCategory("One Way Tests with Properties")]
        public void TestOneWayReceive_XML_PromoteMQAdapterProperties()
        {
            const string adapterNamespace = "http://schemas.microsoft.com/BizTalk/2003/mqs-properties";

            string[] propertyArray = new string[]{
                "MQSeries.MQMD_ApplIdentityData=SomeIdentityData",
                "MQSeries.MQMD_ApplOriginData=SAP123",
                "MQSeries.MQMD_CorrelId=TestCorrelationToken",
                "MQSeries.MQMD_Encoding=Binary",
                "MQSeries.MQMD_Expiry=2017-09-05T12:00:00",
                "MQSeries.MQMD_Format=Text",
                "MQSeries.MQMD_GroupId=",
                "MQSeries.MQMD_MsgId=TestMessageId",
                "MQSeries.MQMD_MsgSeqNumber=123",
                "MQSeries.MQMD_MsgType=Normal",
                "MQSeries.MQMD_Offset=0",
                "MQSeries.MQMD_OriginalLength=123456"
            };

            string properties = string.Join(";", propertyArray);
            //Initializing the inbound handler again and passing the desired property            
            InitInboundHandler(
                string.Format(
                    "mock://localhost/TestEndpoint{0}",
                    endpointId),
                properties);

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            //TODO: implement sending XML message to the inbound handler
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");

            //Verify the promoted properties
            var promotedProperties = msg.Properties
                .Where(p => p.Key == "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote")
                .Single();
                
            Assert.IsNotNull(promotedProperties, "The promoted properties list is not present");

            var propertiesList = promotedProperties.Value as List<KeyValuePair<XmlQualifiedName, object>>;

            Assert.AreEqual(propertiesList.Count, propertyArray.Length, "The element count in the promoted properties list differ");                
                    
            for (int i = 0; i < propertiesList.Count; i++)
            {
                string[] propertyDetails = propertyArray[i].Split('=');

                MessagePropertyValidator.
                    ValidatePromotedProperty(propertiesList[i],
                        adapterNamespace,
                        propertyDetails[0].Replace("MQSeries.", string.Empty),
                        propertyDetails[1]);                    
            }                      
        }

        [TestMethod]
        [TestCategory("One Way Tests with Properties")]
        public void TestOneWayReceive_XML_PromoteMSMQAdapterProperties()
        {
            const string adapterNamespace = "http://schemas.microsoft.com/BizTalk/2003/msmq-properties";

            string[] propertyArray = new string[]{
                "MSMQ.AppSpecific=111",
                "MSMQ.CertificateThumbPrint=bca089fff1234bcdea00009f9747",
                "MSMQ.CorrelationId=TestCorrelationToken",
                "MSMQ.Label=TestLable",
                "MSMQ.Priority=1"
            };

            string properties = string.Join(";", propertyArray);
            //Initializing the inbound handler again and passing the desired property
            InitInboundHandler(
                string.Format(
                    "mock://localhost/TestEndpoint{0}",
                    endpointId),
                properties);

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));
            
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");

            //Verify the promoted properties
            var promotedProperties = msg.Properties
                .Where(p => p.Key == "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote")
                .Single();

            Assert.IsNotNull(promotedProperties, "The promoted properties list is not present");

            var propertiesList = promotedProperties.Value as List<KeyValuePair<XmlQualifiedName, object>>;

            Assert.AreEqual(propertiesList.Count, propertyArray.Length, "The element count in the promoted properties list differ");

            for (int i = 0; i < propertiesList.Count; i++)
            {
                string[] propertyDetails = propertyArray[i].Split('=');

                MessagePropertyValidator
                    .ValidatePromotedProperty(propertiesList[i],
                    adapterNamespace,
                    propertyDetails[0].Replace("MSMQ.", string.Empty),
                    propertyDetails[1]);
            }        
        }

        [TestMethod]
        [TestCategory("One Way Tests with Properties")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestOneWayReceive_XML_PromoteImaginaryAdapterProperties()
        {
            //Initializing the inbound handler again and passing the desired property            
            InitInboundHandler(
                string.Format(
                    "mock://localhost/TestEndpoint{0}",
                    endpointId),
                @"PING.RetryCount=10");

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            //TODO: implement sending XML message to the inbound handler
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");

            //Verify the promoted properties
            var promotedProperties = msg.Properties
                .Where(p => p.Key == "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote")
                .Single();                        
        }
        #endregion

        #region Message property promotion tests
        [TestMethod]
        [TestCategory("One Way Tests with Properties")]
        public void TestOneWayReceive_XML_PromoteCustomProperty()
        {
            //Initializing the inbound handler again and passing the desired property
            //InitInboundHandler(
            //    string.Format(
            //        "mock://localhost/TestEndpoint{0}",
            //        endpointId),
            //    @"FILE.ReceivedFileName=C:\Test\In\File1.xml");

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            //TODO: implement sending XML message to the inbound handler
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;
                mockMessage.Properties.Add("http://www.example.com/custom-props#MyProp", "SomeValue");

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");

            //Verify the promoted properties
            var promotedProperties = msg.Properties
                .Where(p => p.Key == "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote")
                .SingleOrDefault();

            Assert.IsNotNull(promotedProperties, "The promoted properties list is not present");

            var propertiesList = promotedProperties.Value as List<KeyValuePair<XmlQualifiedName, object>>;

            Assert.AreEqual(propertiesList.Count, 1, "The element count in the promoted properties list differ");

            MessagePropertyValidator.
                ValidatePromotedProperty(propertiesList[0],
                    "http://www.example.com/custom-props",
                    "MyProp",
                    @"SomeValue");
        }

        [TestMethod]
        [TestCategory("One Way Tests with Properties")]
        public void TestOneWayReceive_XML_PromoteFileAdapterProperty_MessageOverride()
        {
            //Initializing the inbound handler again and passing the desired property
            InitInboundHandler(
                string.Format(
                    "mock://localhost/TestEndpoint{0}",
                    endpointId),
                @"FILE.ReceivedFileName=C:\Test\In\File1.xml");

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            //TODO: implement sending XML message to the inbound handler
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;
                mockMessage.Properties.Add("FILE.ReceivedFileName", @"C:\Test\In\File22.xml");

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                pipeClient.Close();
            }
            //Now we read the message in the inbound handler
            Message msg = null;
            IInboundReply reply;
            inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
            //Sending empty message to emulate the exact behavior of BizTalk for one way communication
            reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

            Assert.IsNotNull(msg, "Message instance was not returned");
            Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");

            //Verify the promoted properties
            var promotedProperties = msg.Properties
                .Where(p => p.Key == "http://schemas.microsoft.com/BizTalk/2006/01/Adapters/WCF-properties/Promote")
                .SingleOrDefault();

            Assert.IsNotNull(promotedProperties, "The promoted properties list is not present");

            var propertiesList = promotedProperties.Value as List<KeyValuePair<XmlQualifiedName, object>>;

            Assert.AreEqual(propertiesList.Count, 1, "The element count in the promoted properties list differ");

            MessagePropertyValidator.
                ValidatePromotedProperty(propertiesList[0],
                "http://schemas.microsoft.com/BizTalk/2003/file-properties",
                "ReceivedFileName",
                @"C:\Test\In\File22.xml");
        }
        #endregion

        #region Two way tests
        [TestMethod]
        [TestCategory("Two Way Tests")]
        public void TestTwoWayReceive_XML()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                var mockMessage = new MockMessage();
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);

                Assert.IsNotNull(msg, "Message instance was not returned");
                Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");
                //we send the response message
                string responseXml = "<Response><Description>The request was successfully processed</Description></Response>";
                Message responseMessage = GeneralTestHelper.CreateMessageWithBase64EncodedBody(responseXml, Encoding.UTF8);

                MockMessage receivedResponse = null;
                System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
                //We queue up the reply so that it executes from the context of another thread.
                System.Threading.ThreadPool.QueueUserWorkItem(cb =>
                    {
                        receivedResponse = TestUtils.ReceiveResponse(pipeClient);
                        manualEvent.Set();
                    }
                );

                reply.Reply(responseMessage, new TimeSpan(0, 0, 10));
                //We wait for the event to be signalled
                manualEvent.WaitOne(10000);

                Assert.IsNotNull(receivedResponse, "The response message is null!");   
                Assert.AreEqual(responseXml, receivedResponse.Body, "The received response is not correct");                
            }
        }       

        [TestMethod]
        [TestCategory("Two Way Tests")]
        public void TestTwoWayReceive_XML_Unicode()
        {
            adapter.Encoding = "Unicode";
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));            

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                var mockMessage = new MockMessage(Encoding.Unicode);
                mockMessage.Body = xml;

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(memStream, mockMessage);

                    pipeClient.Connect(10000);
                    pipeClient.Write(memStream.ToArray(), 0, (int)memStream.Length);
                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }
                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);

                Assert.IsNotNull(msg, "Message instance was not returned");
                Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.Unicode), "Message contents of received message is different");
                //we send the response message
                string responseXml = "<Response><Description>The request was successfully processed</Description></Response>";
                Message responseMessage = GeneralTestHelper.CreateMessageWithBase64EncodedBody(responseXml, Encoding.Unicode);

                MockMessage receivedResponse = null;
                System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
                //We queue up the reply so that it executes from the context of another thread.
                System.Threading.ThreadPool.QueueUserWorkItem(cb =>
                    {
                        receivedResponse = TestUtils.ReceiveResponse(pipeClient, Encoding.Unicode);
                        manualEvent.Set();
                    }
                );

                reply.Reply(responseMessage, new TimeSpan(0, 0, 10));
                //We wait for the event to be signalled
                manualEvent.WaitOne(10000);

                Assert.IsNotNull(receivedResponse, "The response message is null!");
                Assert.AreEqual(responseXml, receivedResponse.Body, "The received response is not correct");
            }
        }

        [TestMethod]
        [TestCategory("Two Way Tests")]
        [DeploymentItem(@"TestData\MediumMessage.xml")]
        public void TestTwoWayReceive_XML_MediumRequestResponse()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));            

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                int bytesCountRead = 0;                
                byte[] xmlBytes = new byte[4096];

                int fileLength = (int)File.OpenRead("MediumMessage.xml").Length;

                pipeClient.Connect(10000);

                var mockMessage = new MockMessage(
                    "MediumMessage.xml",
                    Encoding.UTF8);

                using (var memStream = new MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(memStream, mockMessage);
                    memStream.Seek(0, SeekOrigin.Begin);

                    while ((bytesCountRead = memStream.Read(xmlBytes, 0, xmlBytes.Length)) > 0)
                    {
                        pipeClient.Write(xmlBytes, 0, bytesCountRead);
                    }

                    // pipeClient.WriteByte(0x00);
                    pipeClient.WaitForPipeDrain();
                }

                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);

                Assert.IsNotNull(msg, "Message instance was not returned");

                byte[] msgBytes = GeneralTestHelper.GetBodyAsBytes(msg);

                Assert.AreEqual(fileLength, msgBytes.Length, "The message length is not correct");

                string requestMessageHash = GeneralTestHelper.CalculateBytesHash(msgBytes);
                string fileHash = GeneralTestHelper.CalculateFileHash("MediumMessage.xml");

                Assert.AreEqual(fileHash,
                    requestMessageHash, "Message contents of received message is different");
                //we send the response message
                
                Message responseMessage = GeneralTestHelper
                    .CreateMessageWithBase64EncodedBody(
                        File.ReadAllBytes("MediumMessage.xml"));

                MockMessage receivedResponse = null;

                System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);

                System.Threading.ThreadPool.QueueUserWorkItem(cb =>
                    {
                        receivedResponse = TestUtils.ReceiveResponse(pipeClient);

                        manualEvent.Set();
                    }
                );

                reply.Reply(responseMessage, new TimeSpan(0, 0, 10));
                //We wait for the event to be signalled
                manualEvent.WaitOne(10000);

                Assert.IsNotNull(receivedResponse, "Received response is null!");
                string responseHash = GeneralTestHelper.CalculateBytesHash(
                    Convert.FromBase64String(
                        receivedResponse.BodyBase64));

                Assert.AreEqual(fileHash, responseHash,
                    "The received response is not correct");                
            }
        }
        #endregion
       
        private void InitInboundHandler(string address, string adapterProperties)
        {
            connectionUri = new MockAdapterConnectionUri(new Uri(address));
            adapter = new MockAdapter();
            adapter.Encoding = "UTF-8";
                        
            if (!string.IsNullOrEmpty(adapterProperties))
            {
                adapter.PromotedProperties = adapterProperties;
            }

            MockAdapterConnectionFactory connectionFactory = new MockAdapterConnectionFactory(
                connectionUri, null, adapter);
            MockAdapterConnection connection = new MockAdapterConnection(connectionFactory);
            inboundHandler = new MockAdapterInboundHandler(connection, null);
        }        

    }

    
}
