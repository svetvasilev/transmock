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
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransMock.Mockifier.Parser;

using Moq;

namespace TransMock.Mockifier.Parser.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestBindingsParser
    {
        private const string SendPortMockOneWayTransportData = "&lt;CustomProps&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;InboundBodyPathExpression vt=\"8\" /&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;EnableTransaction vt=\"11\"&gt;0&lt;/EnableTransaction&gt;&lt;StaticAction vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;ProxyAddress vt=\"8\" /&gt;&lt;UserName vt=\"8\" /&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyElement&lt;/InboundBodyLocation&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;PropagateFaultMessage vt=\"11\"&gt;-1&lt;/PropagateFaultMessage&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Xml&lt;/InboundNodeEncoding&gt;&lt;ProxyUserName vt=\"8\" /&gt;&lt;IsolationLevel vt=\"8\"&gt;Serializable&lt;/IsolationLevel&gt;&lt;UseSSO vt=\"11\"&gt;0&lt;/UseSSO&gt;&lt;/CustomProps&gt;";

        private const string SendPortMockTwoWayTransportData = "&lt;CustomProps&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;EnableTransaction vt=\"11\"&gt;0&lt;/EnableTransaction&gt;&lt;StaticAction vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;ProxyAddress vt=\"8\" /&gt;&lt;UserName vt=\"8\" /&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;PropagateFaultMessage vt=\"11\"&gt;-1&lt;/PropagateFaultMessage&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;ProxyUserName vt=\"8\" /&gt;&lt;IsolationLevel vt=\"8\"&gt;Serializable&lt;/IsolationLevel&gt;&lt;UseSSO vt=\"11\"&gt;0&lt;/UseSSO&gt;&lt;/CustomProps&gt;";

        private const string ReceiveLocationMockOneWayTransportData = "&lt;CustomProps&gt;&lt;OrderedProcessing vt=\"11\"&gt;0&lt;/OrderedProcessing&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseBodyElement&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;DisableLocationOnFailure vt=\"11\"&gt;0&lt;/DisableLocationOnFailure&gt;&lt;UserName vt=\"8\" /&gt;&lt;ServiceBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"ServiceBehavior\" /&amp;gt;&lt;/ServiceBehaviorConfiguration&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"xml\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;IncludeExceptionDetailInFaults vt=\"11\"&gt;-1&lt;/IncludeExceptionDetailInFaults&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;CredentialType vt=\"8\"&gt;None&lt;/CredentialType&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" PromotedProperties=\"{PromotedProperties}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;SuspendMessageOnFailure vt=\"11\"&gt;-1&lt;/SuspendMessageOnFailure&gt;&lt;/CustomProps&gt;";

        private const string ReceiveLocationMockTwoWayTransportData = "&lt;CustomProps&gt;&lt;OrderedProcessing vt=\"11\"&gt;0&lt;/OrderedProcessing&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;DisableLocationOnFailure vt=\"11\"&gt;0&lt;/DisableLocationOnFailure&gt;&lt;UserName vt=\"8\" /&gt;&lt;ServiceBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"ServiceBehavior\" /&amp;gt;&lt;/ServiceBehaviorConfiguration&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;IncludeExceptionDetailInFaults vt=\"11\"&gt;-1&lt;/IncludeExceptionDetailInFaults&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;CredentialType vt=\"8\"&gt;None&lt;/CredentialType&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" PromotedProperties=\"{PromotedProperties}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;SuspendMessageOnFailure vt=\"11\"&gt;-1&lt;/SuspendMessageOnFailure&gt;&lt;/CustomProps&gt;";

        private const string GeneratedClassContents = "\r\nnamespace TestApplication.Test {\r\n\tpublic static class TestApplicationMockAddresses {\r\n\t\tpublic static string OneWaySendFILE\r\n\t\t{\r\n\t\t\tget\r\n\t\t\t{\r\n\t\t\t\treturn \"mock://localhost/OneWaySendFILE\";\r\n\t\t\t}\r\n\t\t}\r\n\r\n\t\tpublic static string TwoWayTestSendWCF\r\n\t\t{\r\n\t\t\tget\r\n\t\t\t{\r\n\t\t\t\treturn \"mock://localhost/TwoWayTestSendWCF\";\r\n\t\t\t}\r\n\t\t}\r\n\r\n\t\tpublic static string OneWayReceive_FILE\r\n\t\t{\r\n\t\t\tget\r\n\t\t\t{\r\n\t\t\t\treturn \"mock://localhost/OneWayReceive_FILE\";\r\n\t\t\t}\r\n\t\t}\r\n\r\n\t\tpublic static string TwoWayTestReceive_WCF\r\n\t\t{\r\n\t\t\tget\r\n\t\t\t{\r\n\t\t\t\treturn \"mock://localhost/TwoWayTestReceive_WCF\";\r\n\t\t\t}\r\n\t\t}\r\n\r\n\t}\r\n}";

        public TestBindingsParser()
        {
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
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        [DeploymentItem(@"TestData\TestApplication.BindingInfo.xml")]
        public void TestInlineParsing_SimpleMock()
        {
            BizTalkBindingsParser parser = new BizTalkBindingsParser();

            parser.ParseBindings("TestApplication.BindingInfo.xml", "TestApplication.BindingInfo_parsed.xml");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfo_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants().Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                string sendPortName = sendPortElement.Attribute("Name").Value;
                bool isTwoWay = bool.Parse(sendPortElement.Attribute("IsTwoWay").Value);
                //fetch the primary transport element
                var primaryTransportElement = sendPortElement.Element("PrimaryTransport");                
                //assert the address
                string address = primaryTransportElement.Element("Address").Value;

                Assert.AreEqual(string.Format("mock://localhost/{0}", sendPortName), address, "The address is not correct");
                //Assert the transport type settings
                var transportTypeElement = primaryTransportElement.Element("TransportType");

                Assert.AreEqual(transportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the send handler transport type settings
                var handlerTransportTypeElement = primaryTransportElement.Descendants()
                    .Where(e => e.Name == "TransportType" && e.Parent.Name == "SendHandler").First();

                Assert.AreEqual(handlerTransportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the transport type data
                var transportData = primaryTransportElement.Element("TransportTypeData");
                if (isTwoWay)
	            {
                    Assert.AreEqual(transportData.Value, SendPortMockTwoWayTransportData.Replace("{Encoding}", "UTF-8"), "Transport type data is not correct");
	            }
                else
                    Assert.AreEqual(transportData.Value, SendPortMockOneWayTransportData.Replace("{Encoding}", "UTF-8"), "Transport type data is not correct");               
                
            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants().Where(e => e.Name == "ReceiveLocation");

            foreach (var receiveLocationElement in receiveLocationElements)
            {
                string receiveLocationName = receiveLocationElement.Attribute("Name").Value;
                bool isTwoWay = bool.Parse(receiveLocationElement.Parent.Parent.Attribute("IsTwoWay").Value);
                //fetch the primary transport element
               // var receiveLocationTransportElement = receiveLocationElement.Element("ReceiveLocationTransportType");
                //assert the address
                string address = receiveLocationElement.Element("Address").Value;

                Assert.AreEqual(string.Format("mock://localhost/{0}", receiveLocationName), address, "The address is not correct");
                //Assert the transport type settings
                var transportTypeElement = receiveLocationElement.Element("ReceiveLocationTransportType");

                Assert.AreEqual(transportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the receive handler transport type settings
                var handlerTransportTypeElement = receiveLocationElement.Descendants()
                    .Where(e => e.Name == "TransportType" && e.Parent.Name == "ReceiveHandler").First();

                Assert.AreEqual(handlerTransportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");
                //Assert the transport type data
                var transportData = receiveLocationElement.Element("ReceiveLocationTransportTypeData");
                string expectedTransportData = null;
                if (isTwoWay)
                {
                    expectedTransportData = ReceiveLocationMockTwoWayTransportData
                        .Replace("{Encoding}", "UTF-8")
                        .Replace("{PromotedProperties}", string.Empty);

                    Assert.AreEqual(expectedTransportData, transportData.Value, "Transport type data is not correct");
                }
                else
                {
                    expectedTransportData = ReceiveLocationMockOneWayTransportData
                        .Replace("{Encoding}", "UTF-8")
                        .Replace("{PromotedProperties}", string.Empty);

                    Assert.AreEqual(expectedTransportData, transportData.Value, "Transport type data is not correct");
                }

            }

            //Verifying the contents of the generated class
            using (System.IO.StreamReader sr = System.IO.File.OpenText("TestApplicationMockAddresses.cs"))
            {
                string classContents = sr.ReadToEnd();

                Assert.AreEqual(GeneratedClassContents, classContents, "The generated MockAddresses class has wrong contents");                
            }
            
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestApplication.BindingInfo.xml")]
        public void TestInlineParsing_SimpleMock_ClassOutputSpecified()
        {
            BizTalkBindingsParser parser = new BizTalkBindingsParser();

            parser.ParseBindings("TestApplication.BindingInfo.xml", "TestApplication.BindingInfo_parsed.xml", ".");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfo_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants().Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                string sendPortName = sendPortElement.Attribute("Name").Value;
                bool isTwoWay = bool.Parse(sendPortElement.Attribute("IsTwoWay").Value);
                //fetch the primary transport element
                var primaryTransportElement = sendPortElement.Element("PrimaryTransport");
                //assert the address
                string address = primaryTransportElement.Element("Address").Value;

                Assert.AreEqual(string.Format("mock://localhost/{0}", sendPortName), address, "The address is not correct");
                //Assert the transport type settings
                var transportTypeElement = primaryTransportElement.Element("TransportType");

                Assert.AreEqual(transportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the send handler transport type settings
                var handlerTransportTypeElement = primaryTransportElement.Descendants()
                    .Where(e => e.Name == "TransportType" && e.Parent.Name == "SendHandler").First();

                Assert.AreEqual(handlerTransportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the transport type data
                var transportData = primaryTransportElement.Element("TransportTypeData");
                if (isTwoWay)
                {
                    Assert.AreEqual(transportData.Value, SendPortMockTwoWayTransportData.Replace("{Encoding}", "UTF-8"), "Transport type data is not correct");
                }
                else
                    Assert.AreEqual(transportData.Value, SendPortMockOneWayTransportData.Replace("{Encoding}", "UTF-8"), "Transport type data is not correct");

            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants().Where(e => e.Name == "ReceiveLocation");

            foreach (var receiveLocationElement in receiveLocationElements)
            {
                string receiveLocationName = receiveLocationElement.Attribute("Name").Value;
                bool isTwoWay = bool.Parse(receiveLocationElement.Parent.Parent.Attribute("IsTwoWay").Value);
                //fetch the primary transport element
                // var receiveLocationTransportElement = receiveLocationElement.Element("ReceiveLocationTransportType");
                //assert the address
                string address = receiveLocationElement.Element("Address").Value;

                Assert.AreEqual(string.Format("mock://localhost/{0}", receiveLocationName), address, "The address is not correct");
                //Assert the transport type settings
                var transportTypeElement = receiveLocationElement.Element("ReceiveLocationTransportType");

                Assert.AreEqual(transportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the receive handler transport type settings
                var handlerTransportTypeElement = receiveLocationElement.Descendants()
                    .Where(e => e.Name == "TransportType" && e.Parent.Name == "ReceiveHandler").First();

                Assert.AreEqual(handlerTransportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");
                //Assert the transport type data
                var transportData = receiveLocationElement.Element("ReceiveLocationTransportTypeData");
                string expectedTransportData = null;
                if (isTwoWay)
                {
                    expectedTransportData = ReceiveLocationMockTwoWayTransportData
                        .Replace("{Encoding}", "UTF-8")
                        .Replace("{PromotedProperties}", string.Empty);

                    Assert.AreEqual(expectedTransportData, transportData.Value, "Transport type data is not correct");
                }
                else
                {
                    expectedTransportData = ReceiveLocationMockOneWayTransportData
                        .Replace("{Encoding}", "UTF-8")
                        .Replace("{PromotedProperties}", string.Empty);

                    Assert.AreEqual(expectedTransportData, transportData.Value, "Transport type data is not correct");
                }

            }

            //Verifying the contents of the generated class
            using (System.IO.StreamReader sr = System.IO.File.OpenText("TestApplicationMockAddresses.cs"))
            {
                string classContents = sr.ReadToEnd();

                Assert.AreEqual(GeneratedClassContents, classContents, "The generated MockAddresses class has wrong contents");
            }

        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestApplication.BindingInfo.xml")]
        public void TestInlineParsing_SimpleMock_MockedFileWriter()
        {
            //Creating a mock for the file writer
            var fileWriterMock = new Mock<IFileWriter>();
            fileWriterMock.Setup(m => m.WriteTextFile(
                It.Is<string>(path => path.EndsWith("TestApplicationMockAddresses.cs")),
                It.Is<string>(contents => contents == GeneratedClassContents)));

            BizTalkBindingsParser parser = new BizTalkBindingsParser(fileWriterMock.Object);

            parser.ParseBindings("TestApplication.BindingInfo.xml", "TestApplication.BindingInfo_parsed.xml");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfo_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants().Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                string sendPortName = sendPortElement.Attribute("Name").Value;
                bool isTwoWay = bool.Parse(sendPortElement.Attribute("IsTwoWay").Value);
                //fetch the primary transport element
                var primaryTransportElement = sendPortElement.Element("PrimaryTransport");
                //assert the address
                string address = primaryTransportElement.Element("Address").Value;

                Assert.AreEqual(string.Format("mock://localhost/{0}", sendPortName), address, "The address is not correct");
                //Assert the transport type settings
                var transportTypeElement = primaryTransportElement.Element("TransportType");

                Assert.AreEqual(transportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the send handler transport type settings
                var handlerTransportTypeElement = primaryTransportElement.Descendants()
                    .Where(e => e.Name == "TransportType" && e.Parent.Name == "SendHandler").First();

                Assert.AreEqual(handlerTransportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the transport type data
                var transportData = primaryTransportElement.Element("TransportTypeData");
                if (isTwoWay)
                {
                    Assert.AreEqual(transportData.Value, SendPortMockTwoWayTransportData.Replace("{Encoding}", "UTF-8"), "Transport type data is not correct");
                }
                else
                    Assert.AreEqual(transportData.Value, SendPortMockOneWayTransportData.Replace("{Encoding}", "UTF-8"), "Transport type data is not correct");

            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants().Where(e => e.Name == "ReceiveLocation");

            foreach (var receiveLocationElement in receiveLocationElements)
            {
                string receiveLocationName = receiveLocationElement.Attribute("Name").Value;
                bool isTwoWay = bool.Parse(receiveLocationElement.Parent.Parent.Attribute("IsTwoWay").Value);
                //fetch the primary transport element
                // var receiveLocationTransportElement = receiveLocationElement.Element("ReceiveLocationTransportType");
                //assert the address
                string address = receiveLocationElement.Element("Address").Value;

                Assert.AreEqual(string.Format("mock://localhost/{0}", receiveLocationName), address, "The address is not correct");
                //Assert the transport type settings
                var transportTypeElement = receiveLocationElement.Element("ReceiveLocationTransportType");

                Assert.AreEqual(transportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the receive handler transport type settings
                var handlerTransportTypeElement = receiveLocationElement.Descendants()
                    .Where(e => e.Name == "TransportType" && e.Parent.Name == "ReceiveHandler").First();

                Assert.AreEqual(handlerTransportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");
                //Assert the transport type data
                var transportData = receiveLocationElement.Element("ReceiveLocationTransportTypeData");
                string expectedTransportData = null;
                if (isTwoWay)
                {
                    expectedTransportData = ReceiveLocationMockTwoWayTransportData
                        .Replace("{Encoding}", "UTF-8")
                        .Replace("{PromotedProperties}", string.Empty);

                    Assert.AreEqual(expectedTransportData, transportData.Value, "Transport type data is not correct");
                }
                else
                {
                    expectedTransportData = ReceiveLocationMockOneWayTransportData
                        .Replace("{Encoding}", "UTF-8")
                        .Replace("{PromotedProperties}", string.Empty);

                    Assert.AreEqual(expectedTransportData, transportData.Value, "Transport type data is not correct");
                }

            }

            //Verifying the contents of the generated class
            fileWriterMock.Verify(m => m.WriteTextFile(
                It.Is<string>(path => path.EndsWith("TestApplicationMockAddresses.cs")),
                It.Is<string>(contents => contents == GeneratedClassContents)), Times.Once());
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestApplication.BindingInfoWithProps.xml")]
        public void TestInlineParsing_WithPromotedProperties()
        {
            BizTalkBindingsParser parser = new BizTalkBindingsParser();

            parser.ParseBindings("TestApplication.BindingInfoWithProps.xml", "TestApplication.BindingInfoWithProps_parsed.xml");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfoWithProps_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants().Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                string sendPortName = sendPortElement.Attribute("Name").Value;
                bool isTwoWay = bool.Parse(sendPortElement.Attribute("IsTwoWay").Value);
                //fetch the primary transport element
                var primaryTransportElement = sendPortElement.Element("PrimaryTransport");
                //assert the address
                string address = primaryTransportElement.Element("Address").Value;

                Assert.AreEqual(string.Format("mock://localhost/{0}", sendPortName), address, "The address is not correct");
                //Assert the transport type settings
                var transportTypeElement = primaryTransportElement.Element("TransportType");

                Assert.AreEqual(transportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the send handler transport type settings
                var handlerTransportTypeElement = primaryTransportElement.Descendants()
                    .Where(e => e.Name == "TransportType" && e.Parent.Name == "SendHandler").First();

                Assert.AreEqual(handlerTransportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the transport type data
                var transportData = primaryTransportElement.Element("TransportTypeData");
                if (isTwoWay)
                {
                    Assert.AreEqual(transportData.Value, SendPortMockTwoWayTransportData.Replace("{Encoding}", "UTF-8"), "Transport type data is not correct");
                }
                else
                    Assert.AreEqual(transportData.Value, SendPortMockOneWayTransportData.Replace("{Encoding}", "UTF-8"), "Transport type data is not correct");

            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants().Where(e => e.Name == "ReceiveLocation");
            
            foreach (var receiveLocationElement in receiveLocationElements)
            {
                string receiveLocationName = receiveLocationElement.Attribute("Name").Value;
                bool isTwoWay = bool.Parse(receiveLocationElement.Parent.Parent.Attribute("IsTwoWay").Value);
                //fetch the primary transport element
                // var receiveLocationTransportElement = receiveLocationElement.Element("ReceiveLocationTransportType");
                //assert the address
                string address = receiveLocationElement.Element("Address").Value;

                Assert.AreEqual(string.Format("mock://localhost/{0}", receiveLocationName), address, "The address is not correct");
                //Assert the transport type settings
                var transportTypeElement = receiveLocationElement.Element("ReceiveLocationTransportType");

                Assert.AreEqual(transportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(transportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");

                //Assert the receive handler transport type settings
                var handlerTransportTypeElement = receiveLocationElement.Descendants()
                    .Where(e => e.Name == "TransportType" && e.Parent.Name == "ReceiveHandler").First();

                Assert.AreEqual(handlerTransportTypeElement.Attribute("Name").Value, "WCF-Custom", "Transport type name is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("Capabilities").Value, "907", "Transport type capabilities is not correct");
                Assert.AreEqual(handlerTransportTypeElement.Attribute("ConfigurationClsid").Value, "af081f69-38ca-4d5b-87df-f0344b12557a", "Transport type configuraiton is not correct");
                //Assert the transport type data
                var transportData = receiveLocationElement.Element("ReceiveLocationTransportTypeData");
                string expectedTransportData = null;
                if (isTwoWay)
                {
                    expectedTransportData = ReceiveLocationMockTwoWayTransportData
                        .Replace("{Encoding}", "UTF-8")
                        .Replace("{PromotedProperties}", string.Empty);

                    Assert.AreEqual(expectedTransportData, transportData.Value, "Transport type data is not correct");
                }
                else
                {
                    expectedTransportData = ReceiveLocationMockOneWayTransportData
                        .Replace("{Encoding}", "UTF-8")
                        .Replace("{PromotedProperties}", "FILE.ReceivedFileName=TestFileName.xml;");

                    Assert.AreEqual(expectedTransportData, transportData.Value, "Transport type data is not correct");
                }                
            }
        }
    }

    internal class MockTag
    {
        public string Encoding { get; set; }

        public string Operation { get; set; }
    }
}
