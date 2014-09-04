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
            InitInboundHandler("mock://localhost/TestEndpoint", null);
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
        public void TestOneWayReceive_XML()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
                //Sending empty message to emulate the exact behavior of BizTalk for one way communication
                reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

                Assert.IsNotNull(msg, "Message instance was not returned");
                Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");
            }
        }

        [TestMethod]
        public void TestOneWayReceive_XML_Unicode()
        {
            adapter.Encoding = "Unicode";
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.Unicode.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
                //Sending empty message to emulate the exact behavior of BizTalk for one way communication
                reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

                Assert.IsNotNull(msg, "Message instance was not returned");
                Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.Unicode), "Message contents of received message is different");
            }
        }

        [Ignore]
        [TestMethod]
        public void TestOneWayReceive_XML_ISO88591()
        {
            adapter.Encoding = "ISO-8859-1";
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
                //Sending empty message to emulate the exact behavior of BizTalk for one way communication
                reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

                Assert.IsNotNull(msg, "Message instance was not returned");
                Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.GetEncoding("ISO-8859-1")), "Message contents of received message is different");
            }
        }

        [TestMethod]
        public void TestOneWayReceive_XML_TwoMessages()
        {
            //TODO: implement sending XML message to the inbound handler
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
                //Sending empty message to emulate the exact behavior of BizTalk for one way communication
                reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

                Assert.IsNotNull(msg, "Message instance was not returned");
                Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received first message is different");
            }

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
                //Sending empty message to emulate the exact behavior of BizTalk for one way communication
                reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

                Assert.IsNotNull(msg, "Message instance was not returned");
                Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received second message is different");
            }
        }        

        [TestMethod]
        public void TestOneWayReceive_FlatFile()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

                byte[] ffBytes = Encoding.UTF8.GetBytes(ffContent);

                pipeClient.Connect(10000);
                pipeClient.Write(ffBytes, 0, ffBytes.Count());
                pipeClient.Flush();
                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);
                //Sending empty message to emulate the exact behavior of BizTalk for one way communication
                reply.Reply(GeneralTestHelper.CreateMessageWithEmptyBody(), TimeSpan.FromSeconds(10));

                Assert.IsNotNull(msg, "Message instance was not returned");
                Assert.AreEqual(ffContent,
                    GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");
                //Assert.AreEqual(string.Format("<FFContent>{0}</FFContent>", Convert.ToBase64String(ffBytes)),
                //    GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");
            }
        }

        [TestMethod]
        public void TestOneWayReceive_FlatFile_Unicode()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

                byte[] ffBytes = Encoding.Unicode.GetBytes(ffContent);

                pipeClient.Connect(10000);
                pipeClient.Write(ffBytes, 0, ffBytes.Count());
                pipeClient.Flush();
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
        }

        [TestMethod]
        public void TestOneWayReceive_FlatFile_ISO88591()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

                byte[] ffBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ffContent);

                pipeClient.Connect(10000);
                pipeClient.Write(ffBytes, 0, ffBytes.Count());
                pipeClient.Flush();
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
        } 
        #endregion

        #region Property promotion tests
        [TestMethod]
        public void TestOneWayReceive_XML_PromoteFileAdapterProperties()
        {
            //Initializing the inbound handler again and passing the desired property
            InitInboundHandler("mock://localhost/TestEndpoint", @"FILE.ReceivedFileName=C:\Test\In\File1.xml");

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
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
                
                VerifyPromotedProperty(propertiesList[0],
                    "http://schemas.microsoft.com/BizTalk/2003/file-properties",
                    "ReceivedFileName",
                    @"C:\Test\In\File1.xml");                
            }
        }

        [TestMethod]
        public void TestOneWayReceive_XML_PromoteFTPAdapterProperties()
        {
            //Initializing the inbound handler again and passing the desired property
            InitInboundHandler("mock://localhost/TestEndpoint", @"FTP.ReceivedFileName=File1.xml");

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
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

                VerifyPromotedProperty(propertiesList[0],
                    "http://schemas.microsoft.com/BizTalk/2003/ftp-properties",
                    "ReceivedFileName",
                    "File1.xml");
            }
        }

        [TestMethod]
        public void TestOneWayReceive_XML_PromotePOP3AdapterProperties()
        {
            const string adapterNamespace = "http://schemas.microsoft.com/BizTalk/2003/pop3-properties";

            string[] propertyArray = new string[]{
                "POP3.From=someone@test.com",
                "POP3.Subject=Test mail",
                "POP3.To=receiver@test.com",
                "POP3.Cc=interested@test.com",
                "POP3.ReplyTo=receiver2@test.com"
            };
            //Initializing the inbound handler again and passing the desired property
            InitInboundHandler("mock://localhost/TestEndpoint", 
                string.Join(";", propertyArray));

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
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

                Assert.AreEqual(propertiesList.Count, 5, "The element count in the promoted properties list differ");

                for (int i = 0; i < propertiesList.Count; i++)
                {
                    string[] propertyDetails = propertyArray[i].Split('=');

                    VerifyPromotedProperty(propertiesList[i],
                        adapterNamespace,
                        propertyDetails[0].Replace("POP3.", string.Empty),
                        propertyDetails[1]);
                }
            }
        }

        [TestMethod]
        public void TestOneWayReceive_XML_PromoteMQAdapterProperties()
        {
            const string adapterNamespace = "http://schemas.microsoft.com/BizTalk/2003/mq-properties";

            string[] propertyArray = new string[]{
                "MQMD.ApplIdentityData=SomeIdentityData",
                "MQMD.ApplOriginData=SAP123",
                "MQMD.CorrelId=TestCorrelationToken",
                "MQMD.Encoding=Binary",
                "MQMD.Expiry=2017-09-05T12:00:00",
                "MQMD.Format=Text",
                "MQMD.GroupID=",
                "MQMD.MsgId=TestMessageId",
                "MQMD.MsgSeqNumber=123",
                "MQMD.MsgType=Normal",
                "MQMD.Offset=0",
                "MQMD.OriginalLength=123456"
            };

            string properties = string.Join(";", propertyArray);
            //Initializing the inbound handler again and passing the desired property
            InitInboundHandler("mock://localhost/TestEndpoint",
               properties);

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
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

                    VerifyPromotedProperty(propertiesList[i],
                        adapterNamespace,
                        propertyDetails[0].Replace('.','_'),
                        propertyDetails[1]);                    
                }              
            }
        }

        [TestMethod]
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
            InitInboundHandler("mock://localhost/TestEndpoint",
               properties);

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
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

                    VerifyPromotedProperty(propertiesList[i],
                        adapterNamespace,
                        propertyDetails[0].Replace("MSMQ.", string.Empty),
                        propertyDetails[1]);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestOneWayReceive_XML_PromoteImaginaryAdapterProperties()
        {
            //Initializing the inbound handler again and passing the desired property
            InitInboundHandler("mock://localhost/TestEndpoint", 
                @"PING.RetryCount=10");

            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
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
        }
        #endregion

        #region Two way tests
        [TestMethod]
        public void TestTwoWayReceive_XML()
        {
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);

                Assert.IsNotNull(msg, "Message instance was not returned");
                Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.UTF8), "Message contents of received message is different");
                //we send the response message
                string responseXml = "<Response><Description>The request was successfully processed</Description></Response>";
                Message responseMessage = GeneralTestHelper.CreateMessageWithBase64EncodedBody(responseXml, Encoding.UTF8);

                System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
                //We queue up the reply so that it executes from the context of another thread.
                System.Threading.ThreadPool.QueueUserWorkItem(cb =>
                    {
                        reply.Reply(responseMessage, new TimeSpan(0, 0, 10));
                        manualEvent.Set();
                    }
                );
                //We wait for the event to be signalled
                manualEvent.WaitOne(10000);
                //we try to read from the pipe
                byte[] inBuffer = new byte[256];
                int bytesCountRead = pipeClient.Read(inBuffer, 0, inBuffer.Length);
                

                //string receivedResponseXml = Encoding.UTF8.GetString(inBuffer, 0, bytesCountRead);
                string receivedResponseXml = GeneralTestHelper.GetMessageFromArray(inBuffer, bytesCountRead, Encoding.UTF8);
                     
                Assert.AreEqual(responseXml, receivedResponseXml, "The received response is not correct");                
            }
        }             

        [TestMethod]
        public void TestTwoWayReceive_XML_Unicode()
        {
            adapter.Encoding = "Unicode";
            inboundHandler.StartListener(null, new TimeSpan(0, 0, 60));            

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost",
                connectionUri.Uri.AbsolutePath, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                //TODO: implement sending XML message to the inbound handler
                string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

                byte[] xmlBytes = Encoding.Unicode.GetBytes(xml);

                pipeClient.Connect(10000);
                pipeClient.Write(xmlBytes, 0, xmlBytes.Count());
                pipeClient.Flush();
                //Now we read the message in the inbound handler
                Message msg = null;
                IInboundReply reply;
                inboundHandler.TryReceive(new TimeSpan(0, 0, 10), out msg, out reply);

                Assert.IsNotNull(msg, "Message instance was not returned");
                Assert.AreEqual(xml, GeneralTestHelper.GetBodyAsString(msg, Encoding.Unicode), "Message contents of received message is different");
                //we send the response message
                string responseXml = "<Response><Description>The request was successfully processed</Description></Response>";
                Message responseMessage = GeneralTestHelper.CreateMessageWithBase64EncodedBody(responseXml, Encoding.Unicode);
                System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
                //We queue up the reply so that it executes from the context of another thread.
                System.Threading.ThreadPool.QueueUserWorkItem(cb =>
                    {
                        reply.Reply(responseMessage, new TimeSpan(0, 0, 10));
                        manualEvent.Set();
                    }
                );
                //We wait for the event to be signalled
                manualEvent.WaitOne(10000);
                //we try to read from the pipe
                byte[] inBuffer = new byte[256];
                int bytesCountRead = pipeClient.Read(inBuffer, 0, inBuffer.Length);
                

                string receivedResponseXml = GeneralTestHelper.GetMessageFromArray(inBuffer, bytesCountRead, Encoding.Unicode);

                Assert.AreEqual(responseXml, receivedResponseXml, "The received response is not correct");
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

        private static void VerifyPromotedProperty(
            KeyValuePair<XmlQualifiedName, object> promotedProperty, 
            string expectedNamespace,
            string expectedName,
            string expectedValue)
        {
            Assert.AreEqual(expectedNamespace, 
                promotedProperty.Key.Namespace,
                "The promoted property namespace differ");
            
            Assert.AreEqual(expectedName, 
                promotedProperty.Key.Name,
                "The promoted property namespace differ");
            
            Assert.AreEqual(expectedValue, 
                promotedProperty.Value, 
                "The value of the promoted property differ");
        }
    }

    
}
