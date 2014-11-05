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
        private const string SendPortOneWayMockTransportKey = "SendPortOneWay";

        private const string SendPortTwoWayMockTransportKey = "SendPortTwoWays";

        private const string ReceiveLocationOneWayMockTransportKey = "ReceiveLocationOneWay";

        private const string ReceiveLocationTwoWayMockTransportKey = "ReceiveLocationTwoWays";

        private const string MockAddressTemplate = "mock://localhost/{0}";

        private const string InprocTransportName="WCF-Custom";

        private const string InprocTransportCapabilities="907";

        private const string InprocTransportConfigurationClsid = "af081f69-38ca-4d5b-87df-f0344b12557a";

        private const string DefaultHostName = "BizTalkServerApplication";

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
        /// Parses bindings for mocked endpoints when the source bintings path and the output bindings path are defined
        /// </summary>
        /// <param name="srcBindingsPath">The source path to the bindings file</param>
        /// <param name="outBindingsPath">The output path to the bindings file</param>
        /// <param name="btsVersion">The version of BizTalk serever the bindings are intended to</param>
        /// <param name="unescape">Flag indicating whether to unscape the transport configuration</param>
        public void ParseBindings(string srcBindingsPath, string outBindingsPath, string btsVersion, bool unescape)
        {
            ParseBindings(srcBindingsPath, outBindingsPath, null, btsVersion, unescape);
        }

        /// <summary>
        /// Parses bindings for mocked endpoints when the source bintings path, the output bindings path and the output class path are defined
        /// </summary>
        /// <param name="srcBindingsPath">The source path to the bindings file</param>
        /// <param name="outBindingsPath">The output path to the bindings file</param>
        /// <param name="outClassPath">The output path to the URL helper class file</param>
        /// <param name="btsVersion">The version of BizTalk serever the bindings are intended to. Default is the latest version.</param>
        /// <param name="unescape">Flag indicating whether to unscape the transport configuration. Default is false</param>
        public void ParseBindings(string srcBindingsPath, 
            string outBindingsPath, 
            string outClassPath, 
            string btsVersion="2013", 
            bool unescape=false)
        {
            XDocument xDoc = XDocument.Load(srcBindingsPath);

            //Mock the send ports
            ParseSendPorts(xDoc.Root, btsVersion, unescape);
            //Mock the receive locations
            ParseReceiveLocations(xDoc.Root, btsVersion, unescape);
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
            StringBuilder helperClassBuilder = new StringBuilder(512);

            helperClassBuilder.AppendLine()
            .AppendFormat("namespace {0}.Test {{",
                applicationName)//Namespace definition start
            //Generating the class definition
            .AppendLine()
                .Append("\t").AppendFormat("public static class {0}MockAddresses {{", 
                    applicationName.Replace(".", string.Empty));//URL helper class definition start

            foreach (var mockEndpoint in endpointMockUrls)
            {
                helperClassBuilder.AppendLine();
                helperClassBuilder.Append("\t\t");
                helperClassBuilder.AppendFormat("public static string {0}",
                    mockEndpoint.Key)//Property definition start
                .AppendLine()
                    .Append("\t\t").Append("{")
                    .AppendLine()
                        .Append("\t\t\t").Append("get")//Getter definition start
                        .AppendLine()
                            .Append("\t\t\t").Append("{")//Opening the property getter
                            .AppendLine()
                                .Append("\t\t\t\t").AppendFormat("return \"{0}\";",
                                    mockEndpoint.Value)//The getter body
                                .AppendLine()                                
                            .Append("\t\t\t").Append("}")//Closing the property getter
                        .AppendLine()
                    .Append("\t\t").Append("}")//Closing the property
                    .AppendLine();
            }

            helperClassBuilder.AppendLine()
                .Append("\t").Append("}")//Closing the class
            .AppendLine()
            .Append("}");//Closing the namespace

            if (!Directory.Exists(classFilePath))
            {
                //In case the path is not a directory, we get the path to it.
                classFilePath = Path.GetDirectoryName(classFilePath);
            }
                       
            classFilePath = Path.Combine(classFilePath, applicationName + "MockAddresses");
            classFilePath = Path.ChangeExtension(classFilePath, "cs");
            
            //Saving the class to a file
            if (fileWriter != null)
            {
                fileWriter.WriteTextFile(Path.ChangeExtension(classFilePath, "cs"), helperClassBuilder.ToString());
            }
        }

        private void ParseReceiveLocations(XElement root, string btsVersion, bool unescape)
        {
            var receiveLocationsWithComments = root.DescendantNodes()
                .Where(n => n.NodeType == XmlNodeType.Comment && n.Parent.Name == "ReceiveLocation");

            foreach (var transportComment in receiveLocationsWithComments)
            {
                System.Diagnostics.Debug.WriteLine("Iterating over comments");

                MockSettings mockSettings = ParseComment(transportComment as XComment);

                if (mockSettings != null)
                {
                    //We fetch the adapter settings in the binding and replace those with the mock ones
                    ReplaceReceiveTransportConfiguration(transportComment.Parent,
                        mockSettings, btsVersion, unescape);
                }
            }
        }

        private void ParseSendPorts(XElement root, string btsVersion, bool unescape)
        {
            var sendPortsComments = root.DescendantNodes()
                .Where(n => n.NodeType == XmlNodeType.Comment && n.Parent.Name == "SendPort");

            foreach (var portComment in sendPortsComments)
            {
                System.Diagnostics.Debug.WriteLine("Iterating over comments");

                MockSettings mockSettings = ParseComment(portComment as XComment);

                if (mockSettings != null)                
                {
                    //Check if the port is static
                    var isDynamicAttribute = portComment.Parent.Attribute("IsStatic");
                    bool isStaticPort = bool.Parse(isDynamicAttribute.Value);

                    if (isStaticPort)
                    {
                        //We fetch the adapter settings in the binding and replace those with the mock ones
                        ReplaceSendTransportConfiguration(
                            portComment.Parent.Element("PrimaryTransport"),
                            mockSettings.Operation, btsVersion, unescape,
                            mockSettings.Encoding ?? "UTF-8");
                    }
                    else
                    {
                        //We process dynamic send ports in a different way
                        ProcessDynamicSendPort(portComment.Parent);
                    }
                    
                }
            }
        }

        private void ProcessDynamicSendPort(XElement dynamciSendPortElement)
        {
            var orchestrations = dynamciSendPortElement.Document.Root
                    .Descendants()
                    .Where(d => d.NodeType == XmlNodeType.Element &&
                        d.Name == "ModuleRef" && d.Attribute("Name").Value == "Orchestrations");

            var sendPortRefElement = orchestrations                
                        .Descendants()
                        .Where(d => d.NodeType == XmlNodeType.Element &&
                            d.Name == "SendPortRef" &&
                            d.Attribute("Name") != null &&
                            d.Attribute("Name").Value == dynamciSendPortElement.Attribute("Name").Value)
                            .SingleOrDefault();

            if (sendPortRefElement != null)
            {
                string logicalPortName = sendPortRefElement.Parent.Attribute("Name").Value;

                //Adding the mock endpoint URL for the dynamic port                 
                endpointMockUrls.Add(logicalPortName,
                    string.Format(MockAddressTemplate, logicalPortName));
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
        

        private void ReplaceSendTransportConfiguration(XElement transportElement, 
            string operation, 
            string btsVersion,
            bool unescape,
            string encoding = "UTF-8")
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

            transportTypeElement.SetAttributeValue(XmlNodeNames.Name, InprocTransportName);
            transportTypeElement.SetAttributeValue(XmlNodeNames.Capabilities, InprocTransportCapabilities);
            transportTypeElement.SetAttributeValue(XmlNodeNames.ConfigurationClsid, InprocTransportConfigurationClsid);

            System.Diagnostics.Debug.WriteLine("Transport type values set");
            #endregion
            
            #region Setting the SendHandler.TransportType element
            var sendHandlerElement = transportElement.Element(XmlNodeNames.SendHandler);

            //Set the host name
            sendHandlerElement.Attribute("Name").Value = DefaultHostName;

            var handlerTransportTypeElement = sendHandlerElement
                .Element(XmlNodeNames.TransportType);

            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.Name, InprocTransportName);
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.Capabilities, InprocTransportCapabilities);
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.ConfigurationClsid, InprocTransportConfigurationClsid);

            System.Diagnostics.Debug.WriteLine("Transport type values set");
            #endregion

            bool isTwoWay = bool.Parse(sendPortElement.Attribute(XmlNodeNames.IsTwoWay).Value);
 	        //we navigate to the element containing the transport configuration
            var transportInfo = transportElement.Element(XmlNodeNames.TransportTypeData);
            
            string mockTransportData = null;
            if (isTwoWay)
            {
                mockTransportData = resourceReader.GetMockTransportConfig(btsVersion,
                    SendPortTwoWayMockTransportKey);

                mockTransportData = mockTransportData.Replace("{Encoding}", encoding);
            }
            else
            {
                mockTransportData = resourceReader.GetMockTransportConfig(btsVersion,
                    SendPortOneWayMockTransportKey);

                mockTransportData = mockTransportData.Replace("{Encoding}", encoding);
            }

            if (unescape)
            {
                mockTransportData = mockTransportData
                    .Replace("&amp;", "&")
                    .Replace("&lt;", "<")
                    .Replace("&gt;", ">");
            }

            transportInfo.Value = mockTransportData;
        }

        private void ReplaceReceiveTransportConfiguration(
            XElement receiveLocationElement, 
            MockSettings mockSettings,
            string btsVersion,
            bool unescape)
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

            #region Setting the ReceiveHandler.TransportType element
            var handlerElement = receiveLocationElement.Descendants()
                .Where(e => e.Name == XmlNodeNames.ReceiveHandler).First();
           
            //Setting the default host name
            handlerElement.Attribute("Name").Value = DefaultHostName;

            var handlerTransportTypeElement = receiveLocationElement.Descendants()
                .Where(e => e.Name == XmlNodeNames.TransportType && e.Parent.Name == XmlNodeNames.ReceiveHandler).First();           
            
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.Name, InprocTransportName);
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.Capabilities, InprocTransportCapabilities);
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.ConfigurationClsid, InprocTransportConfigurationClsid);
           

            System.Diagnostics.Debug.WriteLine("Transport type values set");
            #endregion

            #region Setting the TransportType element
            var transportTypeElement = receiveLocationElement.Element(XmlNodeNames.ReceiveLocationTransportType);

            transportTypeElement.SetAttributeValue(XmlNodeNames.Name, InprocTransportName);
            transportTypeElement.SetAttributeValue(XmlNodeNames.Capabilities, InprocTransportCapabilities);
            transportTypeElement.SetAttributeValue(XmlNodeNames.ConfigurationClsid, InprocTransportConfigurationClsid);

            System.Diagnostics.Debug.WriteLine("Transport type values for Inproc host set");            
            #endregion
            
            bool isTwoWay = bool.Parse(receivePortElement.Attribute(XmlNodeNames.IsTwoWay).Value);
            //we navigate to the element containing the transport configuration
            var transportInfo = receiveLocationElement.Element(XmlNodeNames.ReceiveLocationTransportTypeData);

            //Preparing the proper contents of the transport data element for the mocked transport
            string mockTransportData = null;
            if (isTwoWay)
            {
                mockTransportData = resourceReader.GetMockTransportConfig(btsVersion, 
                    ReceiveLocationTwoWayMockTransportKey);

                mockTransportData = mockTransportData.Replace("{Encoding}", mockSettings.Encoding ?? "UTF-8");                
            }
            else
            {
                mockTransportData = resourceReader.GetMockTransportConfig(btsVersion,
                    ReceiveLocationOneWayMockTransportKey);
                mockTransportData = mockTransportData.Replace("{Encoding}", mockSettings.Encoding ?? "UTF-8");                
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

            if (unescape)
            {
                mockTransportData = mockTransportData
                    .Replace("&amp;", "&")
                    .Replace("&lt;", "<")
                    .Replace("&gt;", ">");
            }

            transportInfo.Value = mockTransportData;
        }
    }
}
