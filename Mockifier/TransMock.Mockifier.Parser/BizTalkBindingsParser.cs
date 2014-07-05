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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.IO;

namespace TransMock.Mockifier.Parser
{
    /// <summary>
    /// This class implements the logic for parsing bindings files by replacing the tagged with the  <![CDATA[<Mock />]]> tag receive location's and send port's transports with the mock adapter
    /// </summary>
    public class BizTalkBindingsParser
    {
        private const string SendPortMockOneWayTransportData = "&lt;CustomProps&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;InboundBodyPathExpression vt=\"8\" /&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;EnableTransaction vt=\"11\"&gt;0&lt;/EnableTransaction&gt;&lt;StaticAction vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;ProxyAddress vt=\"8\" /&gt;&lt;UserName vt=\"8\" /&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyElement&lt;/InboundBodyLocation&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;PropagateFaultMessage vt=\"11\"&gt;-1&lt;/PropagateFaultMessage&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Xml&lt;/InboundNodeEncoding&gt;&lt;ProxyUserName vt=\"8\" /&gt;&lt;IsolationLevel vt=\"8\"&gt;Serializable&lt;/IsolationLevel&gt;&lt;UseSSO vt=\"11\"&gt;0&lt;/UseSSO&gt;&lt;/CustomProps&gt;";

        private const string SendPortMockTwoWayTransportData = "&lt;CustomProps&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;EnableTransaction vt=\"11\"&gt;0&lt;/EnableTransaction&gt;&lt;StaticAction vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;ProxyAddress vt=\"8\" /&gt;&lt;UserName vt=\"8\" /&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;PropagateFaultMessage vt=\"11\"&gt;-1&lt;/PropagateFaultMessage&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;ProxyUserName vt=\"8\" /&gt;&lt;IsolationLevel vt=\"8\"&gt;Serializable&lt;/IsolationLevel&gt;&lt;UseSSO vt=\"11\"&gt;0&lt;/UseSSO&gt;&lt;/CustomProps&gt;";

        private const string ReceiveLocationMockOneWayTransportData = "&lt;CustomProps&gt;&lt;OrderedProcessing vt=\"11\"&gt;0&lt;/OrderedProcessing&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseBodyElement&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;DisableLocationOnFailure vt=\"11\"&gt;0&lt;/DisableLocationOnFailure&gt;&lt;UserName vt=\"8\" /&gt;&lt;ServiceBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"ServiceBehavior\" /&amp;gt;&lt;/ServiceBehaviorConfiguration&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"xml\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;IncludeExceptionDetailInFaults vt=\"11\"&gt;-1&lt;/IncludeExceptionDetailInFaults&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;CredentialType vt=\"8\"&gt;None&lt;/CredentialType&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" PromotedProperties=\"{PromotedProperties}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;SuspendMessageOnFailure vt=\"11\"&gt;-1&lt;/SuspendMessageOnFailure&gt;&lt;/CustomProps&gt;";

        private const string ReceiveLocationMockTwoWayTransportData = "&lt;CustomProps&gt;&lt;OrderedProcessing vt=\"11\"&gt;0&lt;/OrderedProcessing&gt;&lt;InboundBodyLocation vt=\"8\"&gt;UseBodyPath&lt;/InboundBodyLocation&gt;&lt;InboundBodyPathExpression vt=\"8\"&gt;/MessageContent&lt;/InboundBodyPathExpression&gt;&lt;OutboundBodyLocation vt=\"8\"&gt;UseTemplate&lt;/OutboundBodyLocation&gt;&lt;AffiliateApplicationName vt=\"8\" /&gt;&lt;BindingType vt=\"8\"&gt;mockBinding&lt;/BindingType&gt;&lt;DisableLocationOnFailure vt=\"11\"&gt;0&lt;/DisableLocationOnFailure&gt;&lt;UserName vt=\"8\" /&gt;&lt;ServiceBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"ServiceBehavior\" /&amp;gt;&lt;/ServiceBehaviorConfiguration&gt;&lt;EndpointBehaviorConfiguration vt=\"8\"&gt;&amp;lt;behavior name=\"EndpointBehavior\" /&amp;gt;&lt;/EndpointBehaviorConfiguration&gt;&lt;OutboundXmlTemplate vt=\"8\"&gt;&amp;lt;bts-msg-body xmlns=\"http://www.microsoft.com/schemas/bts2007\" encoding=\"base64\"/&amp;gt;&lt;/OutboundXmlTemplate&gt;&lt;IncludeExceptionDetailInFaults vt=\"11\"&gt;-1&lt;/IncludeExceptionDetailInFaults&gt;&lt;InboundNodeEncoding vt=\"8\"&gt;Base64&lt;/InboundNodeEncoding&gt;&lt;CredentialType vt=\"8\"&gt;None&lt;/CredentialType&gt;&lt;BindingConfiguration vt=\"8\"&gt;&amp;lt;binding name=\"mockBinding\" Encoding=\"{Encoding}\" PromotedProperties=\"{PromotedProperties}\" /&amp;gt;&lt;/BindingConfiguration&gt;&lt;SuspendMessageOnFailure vt=\"11\"&gt;-1&lt;/SuspendMessageOnFailure&gt;&lt;/CustomProps&gt;";

        private const string MockAddressTemplate = "mock://localhost/{0}";

        private const string MockTransportName="WCF-Custom";

        private const string MockTransportCapabilities="907";

        private const string MockTransportConfigurationClsid = "af081f69-38ca-4d5b-87df-f0344b12557a";        

        private IResourceReader resourceReader;

        private IFileWriter fileWriter;

        private Dictionary<string, string> endpointMockUrls;

        public BizTalkBindingsParser()
        {
            resourceReader = new ResourceReader();
            fileWriter = new FileWriter();
            endpointMockUrls = new Dictionary<string, string>(5);
        }

        public BizTalkBindingsParser(IResourceReader resourceReader)
        {
            this.resourceReader = resourceReader;
            fileWriter = new FileWriter();
            endpointMockUrls = new Dictionary<string, string>(5);
        }

        public BizTalkBindingsParser(IResourceReader resourceReader, IFileWriter fileWriter)
        {
            this.fileWriter = fileWriter;
            this.resourceReader = resourceReader;
            endpointMockUrls = new Dictionary<string, string>(5);
        }

        public BizTalkBindingsParser(IFileWriter fileWriter)
        {
            this.fileWriter = fileWriter;
            resourceReader = new ResourceReader();
            endpointMockUrls = new Dictionary<string, string>(5);
        }
        /// <summary>
        /// Parses bindings for mocked endpoints when the source bintings path and the output bindings path are defined
        /// </summary>
        /// <param name="srcBindingsPath">The source path to the bindings file</param>
        /// <param name="outBindingsPath">The output path to the bindings file</param>
        public void ParseBindings(string srcBindingsPath, string outBindingsPath)
        {
            ParseBindings(srcBindingsPath, outBindingsPath, null);
        }

        /// <summary>
        /// Parses bindings for mocked endpoints when the source bintings path, the output bindings path and the output class path are defined
        /// </summary>
        /// <param name="srcBindingsPath">The source path to the bindings file</param>
        /// <param name="outBindingsPath">The output path to the bindings file</param>
        /// <param name="outClassPath">The output path to the URL helper class file</param>
        public void ParseBindings(string srcBindingsPath, string outBindingsPath, string outClassPath)
        {
            XDocument xDoc = XDocument.Load(srcBindingsPath);

            //Mock the send ports
            ParseSendPorts(xDoc.Root);
            //Mock the receive locations
            ParseReceiveLocations(xDoc.Root);
            //Save the parsed bindings file
            xDoc.Save(outBindingsPath);
            //Generate the helper class with the mocked urls.
            GenerateURLHelperClass(xDoc.Root, outClassPath ?? outBindingsPath);
        }

        private void GenerateURLHelperClass(XElement root, string classFilePath)
        {
            if (endpointMockUrls.Count == 0)
            {
                return;                
            }
            //getting the BizTalk application name
            string applicationName = root.Descendants().Where(d => d.Name == "ModuleRef" && 
                d.Attributes("Name").First().Value.StartsWith("[Application:")).First().Attribute("Name").Value;

            applicationName = applicationName.Replace("[Application:", string.Empty)
                .Replace("]",string.Empty);

            //Generating the namespace definition
            string helperClassContent = string.Format("\r\nnamespace {0}.Test {{",
                applicationName);
            //Generating the class definition
            helperClassContent += string.Format("\r\n\tpublic static class {0}MockAddresses {{", 
                applicationName.Replace(".", string.Empty));

            foreach (var mockEndpoint in endpointMockUrls)
            {
                helperClassContent += string.Format("\r\n\t\tpublic static string {0}\r\n\t\t{{\r\n\t\t\tget\r\n\t\t\t{{\r\n\t\t\t\treturn \"{1}\";\r\n\t\t\t}}\r\n\t\t}}\r\n", 
                    mockEndpoint.Key, mockEndpoint.Value);                
            }

            helperClassContent += "\r\n\t}\r\n}";

            classFilePath = Path.GetDirectoryName(classFilePath);
            classFilePath = Path.Combine(classFilePath, applicationName + "MockAddresses");
            classFilePath = Path.ChangeExtension(classFilePath, "cs");
            //Saving the class to a file
            if (fileWriter != null)
            {
                fileWriter.WriteTextFile(Path.ChangeExtension(classFilePath, "cs"), helperClassContent);
            }
        }

        private void ParseReceiveLocations(XElement root)
        {
            var receiveLocationsWithComments = root.DescendantNodes().Where(n => n.NodeType == XmlNodeType.Comment && n.Parent.Name == "ReceiveLocation");

            foreach (var transportComment in receiveLocationsWithComments)
            {
                System.Diagnostics.Debug.WriteLine("Iterating over comments");

                MockSettings mockSettings = ParseComment(transportComment as XComment);

                if (mockSettings != null)
                {
                    //We fetch the adapter settings in the binding and replace those with the mock ones
                    ReplaceReceiveTransportConfiguration(transportComment.Parent,
                        mockSettings);
                }
            }
        }

        private void ParseSendPorts(XElement root)
        {
            var sendPortTranpsortsWithComments = root.DescendantNodes().Where(n => n.NodeType == XmlNodeType.Comment && n.Parent.Name == "PrimaryTransport");
                        
            foreach (var transportComment in sendPortTranpsortsWithComments)
            {
                System.Diagnostics.Debug.WriteLine("Iterating over comments");

                MockSettings mockSettings = ParseComment(transportComment as XComment);

                if (mockSettings != null)                
                {                                  
                    //We fetch the adapter settings in the binding and replace those with the mock ones
                    ReplaceSendTransportConfiguration(transportComment.Parent, 
                        mockSettings.Operation, mockSettings.Encoding ?? "UTF-8");
                }
            }
        }

        private MockSettings ParseComment(XComment comment)
        {
            System.Diagnostics.Debug.WriteLine("Parsing a commented send port");
            //Check if the comment is with the correct, expected contents
            //XmlReader xr = comment.CreateReader();
            string commentContent = comment.Value;//xr.ReadOuterXml();

            System.Diagnostics.Debug.WriteLine("Comment content is: " + commentContent);
            //Here we compare the content againse the predefined expected values

            MockSettings mockSettings = null;

            XDocument xMockSettings = XDocument.Parse(commentContent.Trim());
            //Validating the content of the Xml against the Mock schema
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.Add(XmlSchema.Read(resourceReader.MockSchema, null));
            xMockSettings.Validate(schemaSet, null);

            //validation is passed, we deserialize the object
            mockSettings = new MockSettings()
            {
                Encoding = xMockSettings.Root.Attribute(XmlNodeNames.Encoding) == null ? null : xMockSettings.Root.Attribute(XmlNodeNames.Encoding).Value,
                Operation = xMockSettings.Root.Attribute(XmlNodeNames.Operation) == null ? null : xMockSettings.Root.Attribute(XmlNodeNames.Operation).Value
            };
            //TODO: Add logic for parsing PromotedProperties collection
            XElement xPromotedProperties = xMockSettings.Root.Element(XmlNodeNames.PromotedProperties);
            
            if (xPromotedProperties != null)
            {
                var xProperties = xPromotedProperties.Elements(XmlNodeNames.Property);
                //Adding each property to the PromotedProperties collection
                foreach (var xProperty in xProperties)
                {
                    mockSettings.PromotedProperties.Add(xProperty.Attribute(XmlNodeNames.Name).Value, 
                        xProperty.Attribute("Value").Value);                    
                }
            }

            return mockSettings;
        }
        

        private void ReplaceSendTransportConfiguration(XElement transportElement, string operation, string encoding = "UTF-8")
        {
            System.Diagnostics.Debug.WriteLine("Replacing the transport settings");
            //Find out if this is one or two way port
            var sendPortElement = transportElement.Parent;

            #region Setting the Address Element
            //Setting the address
            string sendPortName = sendPortElement.Attribute(XmlNodeNames.Name).Value;

            var addressElement = transportElement.Element(XmlNodeNames.Address);

            string mockAddress = string.Format(MockAddressTemplate, sendPortName);

            if (!string.IsNullOrEmpty(operation))
            {
                mockAddress += string.Format("/{0}", operation);
            }

            addressElement.SetValue(mockAddress);
            
            //Adding the port name and mocked address to the dictionary
            endpointMockUrls.Add(sendPortName, mockAddress);

            System.Diagnostics.Debug.WriteLine("Mock address set to: " + mockAddress);
            #endregion

            #region Setting the TransportType element
            var transportTypeElement = transportElement.Element(XmlNodeNames.TransportType);

            transportTypeElement.SetAttributeValue(XmlNodeNames.Name, MockTransportName);
            transportTypeElement.SetAttributeValue(XmlNodeNames.Capabilities, MockTransportCapabilities);
            transportTypeElement.SetAttributeValue(XmlNodeNames.ConfigurationClsid, MockTransportConfigurationClsid);

            System.Diagnostics.Debug.WriteLine("Transport type values set");
            #endregion

            #region Setting the SendHandler.TransportType element
            var handlerTransportTypeElement = transportElement.Descendants()
                .Where(e => e.Name == XmlNodeNames.TransportType && e.Parent.Name == XmlNodeNames.SendHandler).First();

            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.Name, MockTransportName);
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.Capabilities, MockTransportCapabilities);
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.ConfigurationClsid, MockTransportConfigurationClsid);

            System.Diagnostics.Debug.WriteLine("Transport type values set");
            #endregion

            bool isTwoWay = bool.Parse(sendPortElement.Attribute(XmlNodeNames.IsTwoWay).Value);
 	        //we navigate to the element containing the transport configuration
            var transportInfo = transportElement.Element(XmlNodeNames.TransportTypeData);
            
            string mockTransportData = null;
            if (isTwoWay)
            {
                mockTransportData = SendPortMockTwoWayTransportData.Replace("{Encoding}", encoding);
                transportInfo.SetValue(mockTransportData);
            }
            else
            {
                mockTransportData = SendPortMockOneWayTransportData.Replace("{Encoding}", encoding);
                transportInfo.SetValue(mockTransportData);
            }            
        }

        private void ReplaceReceiveTransportConfiguration(XElement receiveLocationElement, MockSettings mockSettings)
        {
            System.Diagnostics.Debug.WriteLine("Replacing the receive transport settings");
            //Find out if this is one or two way port
            var receivePortElement = receiveLocationElement.Parent.Parent;

            #region Setting the Address Element
            //Setting the address
            string receiveLocationName = receiveLocationElement.Attribute(XmlNodeNames.Name).Value;

            var addressElement = receiveLocationElement.Element(XmlNodeNames.Address);

            string mockAddress = string.Format(MockAddressTemplate, receiveLocationName);

            if (!string.IsNullOrEmpty(mockSettings.Operation))
            {
                mockAddress += string.Format("/{0}", mockSettings.Operation);
            }

            addressElement.SetValue(mockAddress);
            //Adding the receive location name and the mocked address to the dictionary
            endpointMockUrls.Add(receiveLocationName, mockAddress);

            System.Diagnostics.Debug.WriteLine("Mock address set to: " + mockAddress);
            #endregion

            #region Setting the TransportType element
            var transportTypeElement = receiveLocationElement.Element(XmlNodeNames.ReceiveLocationTransportType);

            transportTypeElement.SetAttributeValue(XmlNodeNames.Name, MockTransportName);
            transportTypeElement.SetAttributeValue(XmlNodeNames.Capabilities, MockTransportCapabilities);
            transportTypeElement.SetAttributeValue(XmlNodeNames.ConfigurationClsid, MockTransportConfigurationClsid);

            System.Diagnostics.Debug.WriteLine("Transport type values set");
            #endregion

            #region Setting the ReceiveHandler.TransportType element
            var handlerTransportTypeElement = receiveLocationElement.Descendants()
                .Where(e => e.Name == XmlNodeNames.TransportType && e.Parent.Name == XmlNodeNames.ReceiveHandler).First();

            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.Name, MockTransportName);
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.Capabilities, MockTransportCapabilities);
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.ConfigurationClsid, MockTransportConfigurationClsid);

            System.Diagnostics.Debug.WriteLine("Transport type values set");
            #endregion

            bool isTwoWay = bool.Parse(receivePortElement.Attribute(XmlNodeNames.IsTwoWay).Value);
            //we navigate to the element containing the transport configuration
            var transportInfo = receiveLocationElement.Element(XmlNodeNames.ReceiveLocationTransportTypeData);

            //Preparing the proper contents of the transport data element for the mocked transport
            string mockTransportData = null;
            if (isTwoWay)
            {
                mockTransportData = ReceiveLocationMockTwoWayTransportData.Replace("{Encoding}", mockSettings.Encoding ?? "UTF-8");                
            }
            else
            {
                mockTransportData = ReceiveLocationMockOneWayTransportData.Replace("{Encoding}", mockSettings.Encoding ?? "UTF-8");                
            }
            //Preparing the list of promoted properties if such are defined
            if (mockSettings.PromotedProperties.Count > 0)
            {
                string promotedPropertiesList = null;

                foreach (var promotedProperty in mockSettings.PromotedProperties)
                {
                    promotedPropertiesList += string.Format("{0}={1};", promotedProperty.Key, promotedProperty.Value);                    
                }

                mockTransportData = mockTransportData.Replace("{PromotedProperties}", promotedPropertiesList);
            }
            else
            {
                mockTransportData = mockTransportData.Replace("{PromotedProperties}", string.Empty);
            }

            transportInfo.SetValue(mockTransportData);
        }
    }
}
