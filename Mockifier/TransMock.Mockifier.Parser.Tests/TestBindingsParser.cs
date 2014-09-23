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

        private static Dictionary<string, string> expectedTransportConfig;

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
        [ClassInitialize]
        public static void TestSuiteInitialize(TestContext testContext) 
        {
            if (expectedTransportConfig == null)
                expectedTransportConfig = new Dictionary<string, string>(6);

            if (expectedTransportConfig.Count == 0)
            {
                expectedTransportConfig.Add("BTS2010_ReceiveLocationOneWay",
                    "&lt;CustomProps&gt;&lt;OrderedProcessing vt=\"11\"&gt;0&lt;/OrderedProcessing&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseBodyElement&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;DisableLocationOnFailure vt=\"11\"&gt;0&lt;/DisableLocationOnFailure&gt;&lt;UserName vt=\"8\" /&gt;&lt;ServiceBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"ServiceBehavior\" /&amp;gt;&lt;/ServiceBehaviorConfiguration&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"xml\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;IncludeExceptionDetailInFaults vt=\"11\"&gt;-1&lt;/IncludeExceptionDetailInFaults&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;CredentialType vt=\"8\"&gt;None&lt;/CredentialType&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" PromotedProperties=\"{PromotedProperties}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;SuspendMessageOnFailure vt=\"11\"&gt;-1&lt;/SuspendMessageOnFailure&gt;&lt;/CustomProps&gt;");

                expectedTransportConfig.Add("BTS2010_ReceiveLocationTwoWays",
                    "&lt;CustomProps&gt;&lt;OrderedProcessing vt=\"11\"&gt;0&lt;/OrderedProcessing&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;DisableLocationOnFailure vt=\"11\"&gt;0&lt;/DisableLocationOnFailure&gt;&lt;UserName vt=\"8\" /&gt;&lt;ServiceBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"ServiceBehavior\" /&amp;gt;&lt;/ServiceBehaviorConfiguration&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;IncludeExceptionDetailInFaults vt=\"11\"&gt;-1&lt;/IncludeExceptionDetailInFaults&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;CredentialType vt=\"8\"&gt;None&lt;/CredentialType&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" PromotedProperties=\"{PromotedProperties}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;SuspendMessageOnFailure vt=\"11\"&gt;-1&lt;/SuspendMessageOnFailure&gt;&lt;/CustomProps&gt;");

                expectedTransportConfig.Add("BTS2010_SendPortOneWay",
                    "&lt;CustomProps&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;InboundBodyPathExpression vt=\"8\" /&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;EnableTransaction vt=\"11\"&gt;0&lt;/EnableTransaction&gt;&lt;StaticAction vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;ProxyAddress vt=\"8\" /&gt;&lt;UserName vt=\"8\" /&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyElement&lt;/InboundBodyLocation&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;PropagateFaultMessage vt=\"11\"&gt;-1&lt;/PropagateFaultMessage&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Xml&lt;/InboundNodeEncoding&gt;&lt;ProxyUserName vt=\"8\" /&gt;&lt;IsolationLevel vt=\"8\"&gt;Serializable&lt;/IsolationLevel&gt;&lt;UseSSO vt=\"11\"&gt;0&lt;/UseSSO&gt;&lt;/CustomProps&gt;");

                expectedTransportConfig.Add("BTS2010_SendPortTwoWays",
                    "&lt;CustomProps&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;EnableTransaction vt=\"11\"&gt;0&lt;/EnableTransaction&gt;&lt;StaticAction vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;ProxyAddress vt=\"8\" /&gt;&lt;UserName vt=\"8\" /&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;PropagateFaultMessage vt=\"11\"&gt;-1&lt;/PropagateFaultMessage&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;ProxyUserName vt=\"8\" /&gt;&lt;IsolationLevel vt=\"8\"&gt;Serializable&lt;/IsolationLevel&gt;&lt;UseSSO vt=\"11\"&gt;0&lt;/UseSSO&gt;&lt;/CustomProps&gt;");

                expectedTransportConfig.Add("BTS2013_ReceiveLocationOneWay",
                    "&lt;CustomProps&gt;&lt;BindingConfiguration vt=\"8\"&gt;&lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" PromotedProperties=\"{PromotedProperties}\" /&gt;&lt;/BindingConfiguration&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;DisableLocationOnFailure vt=\"11\"&gt;0&lt;/DisableLocationOnFailure&gt;&lt;UserName vt=\"8\" /&gt;&lt;ServiceBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"ServiceBehavior\" /&amp;gt;&lt;/ServiceBehaviorConfiguration&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"xml\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;IncludeExceptionDetailInFaults vt=\"11\"&gt;-1&lt;/IncludeExceptionDetailInFaults&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;CredentialType vt=\"8\"&gt;None&lt;/CredentialType&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseBodyElement&lt;/OutboundBodyLocation&gt;&lt;SuspendMessageOnFailure vt=\"11\"&gt;-1&lt;/SuspendMessageOnFailure&gt;&lt;OrderedProcessing vt=\"11\"&gt;0&lt;/OrderedProcessing&gt;&lt;/CustomProps&gt;");

                expectedTransportConfig.Add("BTS2013_ReceiveLocationTwoWays",
                    "&lt;CustomProps&gt;&lt;BindingConfiguration vt=\"8\"&gt;&lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" PromotedProperties=\"{PromotedProperties}\" /&gt;&lt;/BindingConfiguration&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;DisableLocationOnFailure vt=\"11\"&gt;0&lt;/DisableLocationOnFailure&gt;&lt;UserName vt=\"8\" /&gt;&lt;ServiceBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"ServiceBehavior\" /&amp;gt;&lt;/ServiceBehaviorConfiguration&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;IncludeExceptionDetailInFaults vt=\"11\"&gt;-1&lt;/IncludeExceptionDetailInFaults&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;CredentialType vt=\"8\"&gt;None&lt;/CredentialType&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;SuspendMessageOnFailure vt=\"11\"&gt;-1&lt;/SuspendMessageOnFailure&gt;&lt;OrderedProcessing vt=\"11\"&gt;0&lt;/OrderedProcessing&gt;&lt;/CustomProps&gt;");

                expectedTransportConfig.Add("BTS2013_SendPortOneWay",
                    "&lt;CustomProps&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;InboundBodyPathExpression vt=\"8\" /&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;StaticAction vt=\"8\" /&gt;&lt;ProxyUserName vt=\"8\" /&gt;&lt;ProxyAddress vt=\"8\" /&gt;&lt;UserName vt=\"8\" /&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyElement&lt;/InboundBodyLocation&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;PropagateFaultMessage vt=\"11\"&gt;-1&lt;/PropagateFaultMessage&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Xml&lt;/InboundNodeEncoding&gt;&lt;IsolationLevel vt=\"8\"&gt;Serializable&lt;/IsolationLevel&gt;&lt;UseSSO vt=\"11\"&gt;0&lt;/UseSSO&gt;&lt;EnableTransaction vt=\"11\"&gt;0&lt;/EnableTransaction&gt;&lt;/CustomProps&gt;");

                expectedTransportConfig.Add("BTS2013_SendPortTwoWays",
                    "&lt;CustomProps&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;BindingConfiguration vt=\"8\"&gt;&lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" /&gt;&lt;/BindingConfiguration&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;StaticAction vt=\"8\" /&gt;&lt;ProxyUserName vt=\"8\" /&gt;&lt;ProxyAddress vt=\"8\" /&gt;&lt;UserName vt=\"8\" /&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;PropagateFaultMessage vt=\"11\"&gt;-1&lt;/PropagateFaultMessage&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;IsolationLevel vt=\"8\"&gt;Serializable&lt;/IsolationLevel&gt;&lt;UseSSO vt=\"11\"&gt;0&lt;/UseSSO&gt;&lt;EnableTransaction vt=\"11\"&gt;0&lt;/EnableTransaction&gt;&lt;/CustomProps&gt;");
            }
        }
        
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
        public void TestInlineParsing_SimpleMock_BTS2010()
        {            
            BizTalkBindingsParser parser = new BizTalkBindingsParser();

            parser.ParseBindings("TestApplication.BindingInfo.xml", "TestApplication.BindingInfo_parsed.xml", "2010");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfo_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                VerifySendPortConfig(sendPortElement, "BTS2010");               
                
            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "ReceiveLocation");

            foreach (var receiveLocationElement in receiveLocationElements)
            {
                VerifyReceiceLocationConfig(receiveLocationElement, "BTS2010");

            }

            //Verifying the contents of the generated class
            using (System.IO.StreamReader sr = System.IO.File.OpenText("TestApplicationMockAddresses.cs"))
            {
                string classContents = sr.ReadToEnd();

                Assert.AreEqual(GeneratedClassContents, classContents, 
                    "The generated MockAddresses class has wrong contents");                
            }
            
        }        

        [TestMethod]
        [DeploymentItem(@"TestData\TestApplication.BindingInfo.xml")]
        public void TestInlineParsing_SimpleMock_ClassOutputSpecified_BTS2010()
        {            
            BizTalkBindingsParser parser = new BizTalkBindingsParser();

            parser.ParseBindings("TestApplication.BindingInfo.xml", 
                "TestApplication.BindingInfo_parsed.xml", ".", "2010");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfo_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                VerifySendPortConfig(sendPortElement, "BTS2010");

            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "ReceiveLocation");

            foreach (var receiveLocationElement in receiveLocationElements)
            {
                VerifyReceiceLocationConfig(receiveLocationElement, "BTS2010");

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
        public void TestInlineParsing_SimpleMock_MockedFileWriter_BTS2010()
        {            
            //Creating a mock for the file writer
            var fileWriterMock = new Mock<IFileWriter>();
            fileWriterMock.Setup(m => m.WriteTextFile(
                It.Is<string>(path => path.EndsWith("TestApplicationMockAddresses.cs")),
                It.Is<string>(contents => contents == GeneratedClassContents)));

            BizTalkBindingsParser parser = new BizTalkBindingsParser(fileWriterMock.Object);

            parser.ParseBindings("TestApplication.BindingInfo.xml", 
                "TestApplication.BindingInfo_parsed.xml", "2010");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfo_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                VerifySendPortConfig(sendPortElement, "BTS2010");

            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "ReceiveLocation");

            foreach (var receiveLocationElement in receiveLocationElements)
            {
                VerifyReceiceLocationConfig(receiveLocationElement, "BTS2010");
            }

            //Verifying the contents of the generated class
            fileWriterMock.Verify(m => m.WriteTextFile(
                It.Is<string>(path => path.EndsWith("TestApplicationMockAddresses.cs")),
                It.Is<string>(contents => contents == GeneratedClassContents)), Times.Once());
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestApplication.BindingInfoWithProps.xml")]
        public void TestInlineParsing_WithPromotedProperties_BTS2010()
        {            
            BizTalkBindingsParser parser = new BizTalkBindingsParser();

            parser.ParseBindings("TestApplication.BindingInfoWithProps.xml", 
                "TestApplication.BindingInfoWithProps_parsed.xml", "2010");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfoWithProps_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                VerifySendPortConfig(sendPortElement, "BTS2010");

            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "ReceiveLocation");

            string expectedPromotedProperties = null;
            foreach (var receiveLocationElement in receiveLocationElements)
            {

                if (receiveLocationElement.Attribute("Name").Value == "OneWayReceive_FILE")
                {
                    expectedPromotedProperties = "FILE.ReceivedFileName=TestFileName.xml;";
                }
                else
                {
                    expectedPromotedProperties = "WCF.FakeAction=TestAction;WCF.FakeAddress=TestAddress;";
                }

                VerifyReceiceLocationConfig(receiveLocationElement, "BTS2010",
                    expectedPromotedProperties);
                
            }
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestApplication.BindingInfo.xml")]
        public void TestInlineParsing_SimpleMock_BTS2013()
        {
            BizTalkBindingsParser parser = new BizTalkBindingsParser();

            parser.ParseBindings("TestApplication.BindingInfo.xml", "TestApplication.BindingInfo2013_parsed.xml", "2013");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfo2013_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                VerifySendPortConfig(sendPortElement, "BTS2013");

            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "ReceiveLocation");

            foreach (var receiveLocationElement in receiveLocationElements)
            {
                VerifyReceiceLocationConfig(receiveLocationElement, "BTS2013");

            }

            //Verifying the contents of the generated class
            using (System.IO.StreamReader sr = System.IO.File.OpenText("TestApplicationMockAddresses.cs"))
            {
                string classContents = sr.ReadToEnd();

                Assert.AreEqual(GeneratedClassContents, classContents,
                    "The generated MockAddresses class has wrong contents");
            }

        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestApplication.BindingInfo.xml")]
        public void TestInlineParsing_SimpleMock_ClassOutputSpecified_BTS2013()
        {
            BizTalkBindingsParser parser = new BizTalkBindingsParser();

            parser.ParseBindings("TestApplication.BindingInfo.xml",
                "TestApplication.BindingInfo2013_parsed.xml", ".", "2013");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfo2013_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                VerifySendPortConfig(sendPortElement, "BTS2013");

            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "ReceiveLocation");

            foreach (var receiveLocationElement in receiveLocationElements)
            {
                VerifyReceiceLocationConfig(receiveLocationElement, "BTS2013");

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
        public void TestInlineParsing_SimpleMock_MockedFileWriter_BTS2013()
        {
            //Creating a mock for the file writer
            var fileWriterMock = new Mock<IFileWriter>();
            fileWriterMock.Setup(m => m.WriteTextFile(
                It.Is<string>(path => path.EndsWith("TestApplicationMockAddresses.cs")),
                It.Is<string>(contents => contents == GeneratedClassContents)));

            BizTalkBindingsParser parser = new BizTalkBindingsParser(fileWriterMock.Object);

            parser.ParseBindings("TestApplication.BindingInfo.xml",
                "TestApplication.BindingInfo2013_parsed.xml", "2013");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfo2013_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                VerifySendPortConfig(sendPortElement, "BTS2013");

            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "ReceiveLocation");

            foreach (var receiveLocationElement in receiveLocationElements)
            {
                VerifyReceiceLocationConfig(receiveLocationElement, "BTS2013");
            }

            //Verifying the contents of the generated class
            fileWriterMock.Verify(m => m.WriteTextFile(
                It.Is<string>(path => path.EndsWith("TestApplicationMockAddresses.cs")),
                It.Is<string>(contents => contents == GeneratedClassContents)), Times.Once());
        }

        [TestMethod]
        [DeploymentItem(@"TestData\TestApplication.BindingInfoWithProps.xml")]
        public void TestInlineParsing_WithPromotedProperties_BTS2013()
        {
            BizTalkBindingsParser parser = new BizTalkBindingsParser();

            parser.ParseBindings("TestApplication.BindingInfoWithProps.xml",
                "TestApplication.BindingInfoWithProps2013_parsed.xml", "2013");

            XDocument parsedBindingsDoc = XDocument.Load("./TestApplication.BindingInfoWithProps2013_parsed.xml");

            //asserting the send ports
            var sendPortElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "SendPort");

            foreach (var sendPortElement in sendPortElements)
            {
                VerifySendPortConfig(sendPortElement, "BTS2013");

            }
            //asserting the receive locations
            var receiveLocationElements = parsedBindingsDoc.Root.Descendants()
                .Where(e => e.Name == "ReceiveLocation");

            string expectedPromotedProperties = null;
            foreach (var receiveLocationElement in receiveLocationElements)
            {

                if (receiveLocationElement.Attribute("Name").Value == "OneWayReceive_FILE")
                {
                    expectedPromotedProperties = "FILE.ReceivedFileName=TestFileName.xml;";
                }
                else
                {
                    expectedPromotedProperties = "WCF.FakeAction=TestAction;WCF.FakeAddress=TestAddress;";
                }

                VerifyReceiceLocationConfig(receiveLocationElement, "BTS2013",
                    expectedPromotedProperties);

            }
        }

        private void VerifyReceiceLocationConfig(XElement receiveLocationElement, string btsVersion, string promotedProperties=null)
        {
            string receiveLocationName = receiveLocationElement.Attribute("Name").Value;
            bool isTwoWay = bool.Parse(receiveLocationElement.Parent.Parent.Attribute("IsTwoWay").Value);
            //fetch the primary transport element
            // var receiveLocationTransportElement = receiveLocationElement.Element("ReceiveLocationTransportType");
            //assert the address
            string address = receiveLocationElement.Element("Address").Value;

            Assert.AreEqual(string.Format("mock://localhost/{0}", receiveLocationName), address, "The address is not correct");

            var receiveHandler = receiveLocationElement.Element("ReceiveHandler");
            bool isHostIsolated = receiveHandler.Attribute("Name").Value
                .ToLower().Contains("isolated"); 
            //Assert the transport type settings
            var transportTypeElement = receiveLocationElement.Element("ReceiveLocationTransportType");

            VerifyReceiveLocationHandler(transportTypeElement, isHostIsolated);

            //Assert the receive handler transport type settings
            var handlerTransportTypeElement = receiveHandler.Element("TransportType");

            VerifyReceiveLocationHandler(handlerTransportTypeElement, isHostIsolated);
            //Assert the transport type data
            var transportData = receiveLocationElement.Element("ReceiveLocationTransportTypeData");
            string expectedTransportData = null;

            string transportDataKey = null;
            if (isTwoWay)
            {
                transportDataKey = string.Format("{0}_ReceiveLocationTwoWays", btsVersion);
            }
            else
            {
                transportDataKey = string.Format("{0}_ReceiveLocationOneWay", btsVersion);
            }

            expectedTransportData = expectedTransportConfig[transportDataKey];
            expectedTransportData = expectedTransportData
                   .Replace("{Encoding}", "UTF-8")
                   .Replace("{PromotedProperties}", promotedProperties ?? string.Empty);

            Assert.AreEqual(expectedTransportData, transportData.Value, 
                "Transport type data is not correct for receive location:" + receiveLocationName);
        }

        private void VerifyReceiveLocationHandler(XElement configElement, bool isHostIsolated)
        {
            if (isHostIsolated)
            {
                Assert.AreEqual("WCF-CustomIsolated",
                configElement.Attribute("Name").Value,
                "Transport type name is not correct");

                Assert.AreEqual("641",
                    configElement.Attribute("Capabilities").Value,
                    "Transport type capabilities is not correct");

                Assert.AreEqual("16824334-968f-42db-b33b-6f8d62ed1ebc",
                    configElement.Attribute("ConfigurationClsid").Value,
                    "Transport type configuraiton is not correct");
            }
            else
            {
                Assert.AreEqual("WCF-Custom",
                configElement.Attribute("Name").Value,
                "Transport type name is not correct");

                Assert.AreEqual("907",
                    configElement.Attribute("Capabilities").Value,
                    "Transport type capabilities is not correct");

                Assert.AreEqual("af081f69-38ca-4d5b-87df-f0344b12557a",
                    configElement.Attribute("ConfigurationClsid").Value,
                    "Transport type configuraiton is not correct");
            }            
        }

        private void VerifySendPortConfig(XElement sendPortElement, string btsVersion)
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

            VerifySendPortHandlerConfig(transportTypeElement);

            //Assert the send handler transport type settings
            var handlerTransportTypeElement = primaryTransportElement.Descendants()
                .Where(e => e.Name == "TransportType" && e.Parent.Name == "SendHandler").First();

            VerifySendPortHandlerConfig(handlerTransportTypeElement);

            //Assert the transport type data
            string transportDataKey = null;
            var transportData = primaryTransportElement.Element("TransportTypeData");
            if (isTwoWay)
            {
                transportDataKey = string.Format("{0}_SendPortTwoWays", btsVersion);
            }
            else
            {
                transportDataKey = string.Format("{0}_SendPortOneWay", btsVersion);
            }

            string expectedTransportData = expectedTransportConfig[transportDataKey];

            Assert.AreEqual(transportData.Value, 
                expectedTransportData.Replace("{Encoding}", "UTF-8"), 
                "Transport type data is not correct for send port:" + sendPortName);
        }

        private void VerifySendPortHandlerConfig(XElement configElement)
        {
            Assert.AreEqual("WCF-Custom",
                configElement.Attribute("Name").Value,
                "Transport type name is not correct");

            Assert.AreEqual("907",
                configElement.Attribute("Capabilities").Value,
                "Transport type capabilities is not correct");

            Assert.AreEqual("af081f69-38ca-4d5b-87df-f0344b12557a",
                configElement.Attribute("ConfigurationClsid").Value,
                "Transport type configuraiton is not correct");
        }
    }

    internal class MockTag
    {
        public string Encoding { get; set; }

        public string Operation { get; set; }
    }
}
