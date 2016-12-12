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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace TransMock.Mockifier.Parser
{
    /// <summary>
    /// This class implements the logic for parsing bindings files by replacing the tagged with the  <![CDATA[<Mock />]]> tag receive location's and send port's transports with the mock adapter
    /// </summary>
    public class BizTalkBindingsParser
    {
        /// <summary>
        /// A constant representing the key to fetch the mock transport configuration for a one-way send port
        /// </summary>
        private const string SendPortOneWayMockTransportKey = "SendPortOneWay";

        /// <summary>
        /// A constant representing the key to fetch the mock transport configuration for a two-way send port
        /// </summary>
        private const string SendPortTwoWayMockTransportKey = "SendPortTwoWays";

        /// <summary>
        /// A constant representing the key to fetch the mock transport configuration for a one-way receive location
        /// </summary>
        private const string ReceiveLocationOneWayMockTransportKey = "ReceiveLocationOneWay";

        /// <summary>
        /// A constant representing the key to fetch the mock configuration for a two-way receive location
        /// </summary>
        private const string ReceiveLocationTwoWayMockTransportKey = "ReceiveLocationTwoWays";

        /// <summary>
        /// A constant representing the template for a mock address
        /// </summary>
        private const string MockAddressTemplate = "mock://localhost/{0}";

        /// <summary>
        /// A constant representing the template for mock address for a dynamic send port
        /// </summary>
        private const string MockDynamicAddressTemplate = "mock://localhost/Dynamic{0}";

        /// <summary>
        /// A constant representing the name of the transport name for an in-process host
        /// </summary>
        private const string InprocTransportName = "WCF-Custom";

        /// <summary>
        /// A constant representing the transport capabilities for an in-process host
        /// </summary>
        private const string InprocTransportCapabilities = "907";

        /// <summary>
        /// A constant representing the transport configuration class id for an in-process host
        /// </summary>
        private const string InprocTransportConfigurationClsid = "af081f69-38ca-4d5b-87df-f0344b12557a";

        /// <summary>
        /// A constant representing the default host name
        /// </summary>
        private const string DefaultHostName = "BizTalkServerApplication";

        /// <summary>
        /// A constant representing the default service endpoint behavior configuration
        /// </summary>
        private const string DefaultServiceBehaviorConfig = "&lt;behavior name=\"ServiceBehavior\" /&gt;";

        /// <summary>
        /// A constant representing the default endpoint behavior configuration
        /// </summary>
        private const string DefaultEndpointBehaviorConfig = "&lt;behavior name=\"EndpointBehavior\" /&gt;";

        /// <summary>
        /// Resource reader used to read embedded resources
        /// </summary>
        private IResourceReader resourceReader;

        /// <summary>
        /// File writer used to write string contents to an external file
        /// </summary>
        private IFileWriter fileWriter;

        /// <summary>
        /// A dictionary containing mapping between the endpoints and their mock URLs
        /// </summary>
        private Dictionary<string, string> endpointMockUrls;

        /// <summary>
        /// Initializes a new instance of the <see cref="BizTalkBindingsParser"/> class
        /// </summary>
        public BizTalkBindingsParser()
        {
            this.resourceReader = new ResourceReader();
            this.fileWriter = new FileWriter();
            this.endpointMockUrls = new Dictionary<string, string>(5);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BizTalkBindingsParser"/> class with a specified instance of a resource reader
        /// </summary>
        /// <param name="resourceReader">The instance of the recourse reader to use for extracting embedded resources</param>
        public BizTalkBindingsParser(IResourceReader resourceReader)
        {
            this.resourceReader = resourceReader;
            this.fileWriter = new FileWriter();
            this.endpointMockUrls = new Dictionary<string, string>(5);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BizTalkBindingsParser"/> class with a specified instance of a file writer
        /// </summary>
        /// <param name="fileWriter">The instance of the file writer to use for writing contents to file</param>
        public BizTalkBindingsParser(IFileWriter fileWriter)
        {
            this.fileWriter = fileWriter;
            this.resourceReader = new ResourceReader();
            this.endpointMockUrls = new Dictionary<string, string>(5);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BizTalkBindingsParser"/> class with a specified instances of a resource reader and a file writer
        /// </summary>
        /// <param name="resourceReader">The instance of the recourse reader to use for extracting embedded resources</param>
        /// <param name="fileWriter">The instance of the file writer to use for writing contents to file</param>
        public BizTalkBindingsParser(IResourceReader resourceReader, IFileWriter fileWriter)
        {
            this.fileWriter = fileWriter;
            this.resourceReader = resourceReader;
            this.endpointMockUrls = new Dictionary<string, string>(5);
        }
        
        /// <summary>
        /// Parses bindings for mocked endpoints when the source bindings path and the output bindings path are defined
        /// </summary>
        /// <param name="srcBindingsPath">The source path to the bindings file</param>
        /// <param name="outBindingsPath">The output path to the bindings file</param>
        public void ParseBindings(string srcBindingsPath, string outBindingsPath)
        {
            this.ParseBindings(srcBindingsPath, outBindingsPath, null);
        }
        
        /// <summary>
        /// Parses bindings for mocked endpoints when the source bindings path and the output bindings path are defined
        /// </summary>
        /// <param name="srcBindingsPath">The source path to the bindings file</param>
        /// <param name="outBindingsPath">The output path to the bindings file</param>
        /// <param name="btsVersion">The version of BizTalk server the bindings are intended to</param>
        /// <param name="unescape">Flag indicating whether to unescape the transport configuration</param>
        public void ParseBindings(string srcBindingsPath, string outBindingsPath, string btsVersion, bool unescape)
        {
            this.ParseBindings(srcBindingsPath, outBindingsPath, null, btsVersion, unescape);
        }

        /// <summary>
        /// Parses bindings for mocked endpoints when the source bindings path, the output bindings path and the output class path are defined
        /// </summary>
        /// <param name="srcBindingsPath">The source path to the bindings file</param>
        /// <param name="outBindingsPath">The output path to the bindings file</param>
        /// <param name="outClassPath">The output path to the URL helper class file</param>
        /// <param name="btsVersion">The version of BizTalk server the bindings are intended to. Default is the latest version.</param>
        /// <param name="unescape">Flag indicating whether to unescape the transport configuration. Default is false</param>
        public void ParseBindings(
            string srcBindingsPath, 
            string outBindingsPath, 
            string outClassPath, 
            string btsVersion = "2013", 
            bool unescape = false)
        {
            XDocument bindingsDoc = XDocument.Load(srcBindingsPath);

            // Mock the send ports
            this.ParseSendPorts(bindingsDoc.Root, btsVersion, unescape);

            // Mock the receive locations
            this.ParseReceiveLocations(bindingsDoc.Root, btsVersion, unescape);

            // Save the parsed bindings file
            bindingsDoc.Save(outBindingsPath);

            // Generate the helper class with the mocked urls.
            this.GenerateURLHelperClass(bindingsDoc.Root, outClassPath ?? outBindingsPath);
        }

        /// <summary>
        /// Unescapes transport configuration string
        /// </summary>
        /// <param name="mockTransportData">The string containing the transport configuration data to unescape</param>
        /// <returns>A string containing the unescaped representation of the transport configuration data passed as a parameter</returns>
        private static string UnescapeTransportConfig(string mockTransportData)
        {
            mockTransportData = mockTransportData
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">");

            return mockTransportData;
        }

        /// <summary>
        /// Generates the contents of the mock URL helper class
        /// </summary>
        /// <param name="root">The root element of the bindings file</param>
        /// <param name="classFilePath">The path to the class file</param>
        private void GenerateURLHelperClass(XElement root, string classFilePath)
        {
            if (this.endpointMockUrls.Count == 0)
            {
                return;                
            }

            // Getting the BizTalk application name
            string applicationName = root.Descendants()
                .Where(d => d.Name == "ModuleRef" && d.Attributes("Name")
                    .First().Value.StartsWith("[Application:"))
                .First().Attribute("Name").Value;

            applicationName = applicationName
                .Replace("[Application:", string.Empty)
                .Replace("]", string.Empty);

            // Generating the namespace definition
            StringBuilder helperClassBuilder = new StringBuilder(512);

            helperClassBuilder.AppendLine()
                .AppendFormat(
                    "namespace {0}.Test",
                    applicationName) // Namespace definition start
                // Generating the class definition
                .AppendLine()
                .AppendLine("{")
                    .Append("\t").AppendFormat(
                        "public static class {0}MockAddresses", 
                        applicationName
                            .Replace(".", string.Empty)
                            .Replace("-", "_"))
                        .AppendLine()
                        .Append("\t{"); // URL helper class definition start

            foreach (var mockEndpoint in this.endpointMockUrls)
            {
                helperClassBuilder.AppendLine();
                helperClassBuilder.Append("\t\t");
                helperClassBuilder.AppendFormat(
                    "public static string {0}",
                    mockEndpoint.Key
                        .Replace(".", "_")
                        .Replace(" ", string.Empty)) // Property definition start
                    .AppendLine()
                        .Append("\t\t").Append("{")
                        .AppendLine()
                            .Append("\t\t\t").Append("get") // Getter definition start
                            .AppendLine()
                                .Append("\t\t\t").Append("{") // Opening the property getter
                                .AppendLine()
                                    .Append("\t\t\t\t").AppendFormat(
                                        "return \"{0}\";",
                                        mockEndpoint.Value) // The getter body
                                    .AppendLine()                                
                                .Append("\t\t\t").Append("}") // Closing the property getter
                            .AppendLine()
                        .Append("\t\t").Append("}") // Closing the property
                        .AppendLine();
            }

            helperClassBuilder.AppendLine()
                .Append("\t").Append("}") // Closing the class
            .AppendLine()
            .Append("}"); // Closing the namespace

            if (!Directory.Exists(classFilePath))
            {
                // In case the path is not a directory, we get the path to it.
                classFilePath = Path.GetDirectoryName(classFilePath);
            }
                       
            classFilePath = Path.Combine(classFilePath, applicationName + "MockAddresses");
            classFilePath = Path.ChangeExtension(classFilePath, "cs");
            
            // Saving the class to a file
            if (this.fileWriter != null)
            {
                this.fileWriter.WriteTextFile(
                    Path.ChangeExtension(classFilePath, "cs"), 
                    helperClassBuilder.ToString());
            }
        }

        /// <summary>
        /// Parses the receive locations for any mocking
        /// </summary>
        /// <param name="root">The root element for the receive ports</param>
        /// <param name="btsVersion">The version of the BizTalk server</param>
        /// <param name="unescape">Indicates whether the mock transport configuration should be unescaped</param>
        private void ParseReceiveLocations(XElement root, string btsVersion, bool unescape)
        {
            var receiveLocationsWithComments = root.DescendantNodes()
                .Where(n => n.NodeType == XmlNodeType.Comment && n.Parent.Name == "ReceiveLocation");

            foreach (var transportComment in receiveLocationsWithComments)
            {
                System.Diagnostics.Debug.WriteLine("Iterating over comments");

                MockSettings mockSettings = this.ParseComment(transportComment as XComment);

                if (mockSettings != null)
                {
                    // We fetch the adapter settings in the binding and replace those with the mock ones
                    this.ReplaceReceiveTransportConfiguration(
                        transportComment.Parent,
                        mockSettings, 
                        btsVersion, 
                        unescape);
                }
            }
        }

        /// <summary>
        /// Parses the send ports in the bindings for any mocking
        /// </summary>
        /// <param name="root">The XML root element containing all the send ports configurations</param>
        /// <param name="btsVersion">The version of the BizTalk server</param>
        /// <param name="unescape">A flag indicating whether to unescape the mock transport configuration</param>
        private void ParseSendPorts(XElement root, string btsVersion, bool unescape)
        {
            var sendPortsComments = root.DescendantNodes()
                .Where(n => n.NodeType == XmlNodeType.Comment && n.Parent.Name == "SendPort");

            foreach (var portComment in sendPortsComments)
            {
                System.Diagnostics.Debug.WriteLine("Iterating over comments");

                MockSettings mockSettings = this.ParseComment(portComment as XComment);

                if (mockSettings != null)                
                {
                    // Check if the port is static
                    var isDynamicAttribute = portComment.Parent.Attribute("IsStatic");
                    bool isStaticPort = bool.Parse(isDynamicAttribute.Value);

                    if (isStaticPort)
                    {
                        // We fetch the adapter settings in the binding and replace those with the mock ones
                        this.ReplaceSendTransportConfiguration(
                            portComment.Parent.Element("PrimaryTransport"),
                            mockSettings, 
                            btsVersion,
                            unescape);
                    }
                    else
                    {
                        // We process dynamic send ports in a different way
                        this.ProcessDynamicSendPort(portComment.Parent);
                    }                    
                }
            }
        }

        /// <summary>
        /// Processes dynamic send port configuration
        /// </summary>
        /// <param name="dynamciSendPortElement">The XML element containing the dynamic send port configuration</param>
        private void ProcessDynamicSendPort(XElement dynamciSendPortElement)
        {
            var sendPortRefElement = dynamciSendPortElement.Document.Root                
                        .Descendants()
                        .Where(d => d.NodeType == XmlNodeType.Element   &&
                            d.Name == "SendPortRef"                     &&                            
                            d.Attribute("Name") != null                 &&                            
                            d.Attribute("Name").Value == dynamciSendPortElement.Attribute("Name").Value)
                            .SingleOrDefault();

            if (sendPortRefElement != null)
            {
                string logicalPortName = sendPortRefElement.Parent.Attribute("Name").Value;

                // Adding the mock endpoint URL for the dynamic port                 
                this.endpointMockUrls.Add(
                    string.Format("Dynamic{0}", logicalPortName),
                    string.Format(MockDynamicAddressTemplate, logicalPortName));
            }            
        }

        /// <summary>
        /// Parses an XML comment for instance of Mock tag
        /// </summary>
        /// <param name="comment">The instance of the XML comment to parse</param>
        /// <returns>An instance of the MockSettings class which represents the serialized object of the Mock tag</returns>
        private MockSettings ParseComment(XComment comment)
        {
            System.Diagnostics.Debug.WriteLine("Parsing a commented send port");
            MockSettings mockSettings = null;

            try
            {
                string commentContent = comment.Value;

                System.Diagnostics.Debug.WriteLine("Comment content is: " + commentContent);
                
                // Here we compare the content againse the predefined expected values               
                XDocument mockSettingsDoc = XDocument.Parse(commentContent.Trim());

                // Validating the content of the Xml against the Mock schema
                XmlSchemaSet schemaSet = new XmlSchemaSet();
                schemaSet.Add(XmlSchema.Read(this.resourceReader.MockSchema, null));
                mockSettingsDoc.Validate(schemaSet, null);

                // Validation passed, we deserialize the object
                mockSettings = new MockSettings()
                {
                    EndpointName = mockSettingsDoc.Root.Attribute(XmlNodeNames.EndpointName) == null ?
                        null : mockSettingsDoc.Root.Attribute(XmlNodeNames.EndpointName).Value,
                    Encoding = mockSettingsDoc.Root.Attribute(XmlNodeNames.Encoding) == null ? 
                        null : mockSettingsDoc.Root.Attribute(XmlNodeNames.Encoding).Value,
                    Operation = mockSettingsDoc.Root.Attribute(XmlNodeNames.Operation) == null ? 
                        null : mockSettingsDoc.Root.Attribute(XmlNodeNames.Operation).Value
                };

                // Parsing PromotedProperties collection
                XElement promotedPropertiesElement = mockSettingsDoc.Root.Element(XmlNodeNames.PromotedProperties);

                if (promotedPropertiesElement != null)
                {
                    var propertieElement = promotedPropertiesElement.Elements(XmlNodeNames.Property);
                    
                    // Adding each property to the PromotedProperties collection
                    foreach (var propertyElement in propertieElement)
                    {
                        mockSettings.PromotedProperties.Add(
                            propertyElement.Attribute(XmlNodeNames.Name).Value,
                            propertyElement.Attribute("Value").Value);
                    }
                }

                System.Diagnostics.Debug.WriteLine("ParseComment succeeded. Returning a MockSettings object");                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ParseComments threw an exception: " + ex.Message);
            }

            return mockSettings;
        }
        
        /// <summary>
        /// Replaces the transport configuration for a send port with the mock adapter configuration
        /// </summary>
        /// <param name="transportElement">The XML element of the send port transport configuration</param>
        /// <param name="mockSettings">The mock settings object</param>
        /// <param name="btsVersion">The version of BizTalk server</param>
        /// <param name="unescape">A flag indicating whether to unescape the mock transport configuration</param>
        private void ReplaceSendTransportConfiguration(
            XElement transportElement, 
            MockSettings mockSettings, 
            string btsVersion,
            bool unescape)
        {
            System.Diagnostics.Debug.WriteLine("Replacing the transport settings");
            
            // Find out if this is one or two way port
            var sendPortElement = transportElement.Parent;

            #region Setting the Address Element

            // Setting the address
            string sendPortName = sendPortElement.Attribute(XmlNodeNames.Name).Value;

            var addressElement = transportElement.Element(XmlNodeNames.Address);

            string mockAddress = this.GenerateMockAddress(mockSettings, sendPortName);            

            addressElement.SetValue(mockAddress);
            
            // Adding the port name and mocked address to the dictionary
            this.endpointMockUrls.Add(sendPortName, mockAddress);

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

            // Set the host name
            sendHandlerElement.Attribute("Name").Value = DefaultHostName;

            var handlerTransportTypeElement = sendHandlerElement
                .Element(XmlNodeNames.TransportType);

            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.Name, InprocTransportName);
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.Capabilities, InprocTransportCapabilities);
            handlerTransportTypeElement.SetAttributeValue(XmlNodeNames.ConfigurationClsid, InprocTransportConfigurationClsid);

            System.Diagnostics.Debug.WriteLine("Transport type values set");
            #endregion

            bool isTwoWay = bool.Parse(sendPortElement.Attribute(XmlNodeNames.IsTwoWay).Value);

            // We navigate to the element containing the transport configuration
            var transportInfo = transportElement.Element(XmlNodeNames.TransportTypeData);
            
            string mockTransportData = null;
            if (isTwoWay)
            {
                mockTransportData = this.resourceReader.GetMockTransportConfig(
                    btsVersion,
                    SendPortTwoWayMockTransportKey);

                mockTransportData = mockTransportData.Replace(
                    "{Encoding}", 
                    mockSettings.Encoding ?? "UTF-8");
            }
            else
            {
                mockTransportData = this.resourceReader.GetMockTransportConfig(
                    btsVersion,
                    SendPortOneWayMockTransportKey);

                mockTransportData = mockTransportData.Replace(
                    "{Encoding}",
                    mockSettings.Encoding ?? "UTF-8");
            }

            // Parse the original transport info and extract any custom service behaviors
            string customEndpointBehaviors = this.ExtractCustomEndpointBehaviors(transportInfo.Value);

            // Place any existing custom endpoint behaviors in the mocked transport data, otherwise place the default behaviors
            mockTransportData = mockTransportData.Replace(
                "{EndpointBehavior}",
                !string.IsNullOrEmpty(customEndpointBehaviors) ? customEndpointBehaviors : DefaultEndpointBehaviorConfig);

            // In case unescaping was defined as an option
            if (unescape)
            {
                mockTransportData = UnescapeTransportConfig(mockTransportData);
            }

            // Finally replace the current transport config with the mocked ones
            transportInfo.Value = mockTransportData;
        }

        /// <summary>
        /// Replaces the transport configuration for a receive location with the mock adapter transport
        /// </summary>
        /// <param name="receiveLocationElement">The XML element of the receive location</param>
        /// <param name="mockSettings">The instance of the mock adapter settings</param>
        /// <param name="btsVersion">the version of BizTalk server</param>
        /// <param name="unescape">A flag indicating whether to unescape the mock adapter transport configuration</param>
        private void ReplaceReceiveTransportConfiguration(
            XElement receiveLocationElement, 
            MockSettings mockSettings,
            string btsVersion,
            bool unescape)
        {
            System.Diagnostics.Debug.WriteLine("Replacing the receive transport settings");

            // Find out if this is one or two way port
            var receivePortElement = receiveLocationElement.Parent.Parent;

            #region Setting the Address Element

            // Setting the address
            string receiveLocationName = receiveLocationElement.Attribute(XmlNodeNames.Name).Value;

            var addressElement = receiveLocationElement.Element(XmlNodeNames.Address);

            string mockAddress = this.GenerateMockAddress(mockSettings, receiveLocationName);

            addressElement.SetValue(mockAddress);
            
            // Adding the receive location name and the mocked address to the dictionary
            this.endpointMockUrls.Add(receiveLocationName, mockAddress);

            System.Diagnostics.Debug.WriteLine("Mock address set to: " + mockAddress);
            #endregion

            #region Setting the ReceiveHandler.TransportType element
            var handlerElement = receiveLocationElement.Descendants()
                .Where(e => e.Name == XmlNodeNames.ReceiveHandler).First();
           
            // Setting the default host name
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
            
            // Navigate to the element containing the transport configuration
            var transportInfo = receiveLocationElement.Element(XmlNodeNames.ReceiveLocationTransportTypeData);

            // Preparing the proper contents of the transport data element for the mocked transport
            string mockTransportData = null;
            if (isTwoWay)
            {
                mockTransportData = this.resourceReader.GetMockTransportConfig(
                    btsVersion, 
                    ReceiveLocationTwoWayMockTransportKey);

                mockTransportData = mockTransportData.Replace(
                    "{Encoding}", 
                    mockSettings.Encoding ?? "UTF-8");                
            }
            else
            {
                mockTransportData = this.resourceReader.GetMockTransportConfig(
                    btsVersion,
                    ReceiveLocationOneWayMockTransportKey);

                mockTransportData = mockTransportData.Replace(
                    "{Encoding}", 
                    mockSettings.Encoding ?? "UTF-8");                
            }

            // Preparing the list of promoted properties if such are defined
            if (mockSettings.PromotedProperties.Count > 0)
            {
                string promotedPropertiesList = null;

                foreach (var promotedProperty in mockSettings.PromotedProperties)
                {
                    promotedPropertiesList += string.Format(
                        CultureInfo.InvariantCulture, 
                        "{0}={1};", 
                        promotedProperty.Key, 
                        promotedProperty.Value);                    
                }

                mockTransportData = mockTransportData.Replace(
                    "{PromotedProperties}", 
                    promotedPropertiesList);
            }
            else
            {
                mockTransportData = mockTransportData.Replace(
                    "{PromotedProperties}", 
                    string.Empty);
            }

            // Parse the original transport info and extract any custom service behaviors
            string customServiceBehaviors = this.ExtractCustomServiceBehaviors(transportInfo.Value);

            // Place any existing custom servince behaviors in the mocked transport data, otherwise place the default behaviors
            mockTransportData = mockTransportData.Replace(
                "{ServiceBehavior}",
                !string.IsNullOrEmpty(customServiceBehaviors) ? customServiceBehaviors : DefaultServiceBehaviorConfig);            

            // Parse the original transport info and extract any custom service behaviors
            string customEndpointBehaviors = this.ExtractCustomEndpointBehaviors(transportInfo.Value);

            // Place any existing custom endpoint behaviors in the mocked transport data, otherwise place the default behaviors
            mockTransportData = mockTransportData.Replace(
                "{EndpointBehavior}",
                !string.IsNullOrEmpty(customEndpointBehaviors) ? customEndpointBehaviors : DefaultEndpointBehaviorConfig);

            // In case unescaping was specified as an option
            if (unescape)
            {
                mockTransportData = UnescapeTransportConfig(mockTransportData);
            }
            
            // Finally replace the current transport config with the mocked ones
            transportInfo.Value = mockTransportData;
        }

        /// <summary>
        /// Generates the mock address from the provided mock settings and the endpoint name from the bindings
        /// </summary>
        /// <param name="mockSettings">The mock settings instance</param>
        /// <param name="bindingsEndpointName">The name of the endpoint as defined in the bindings file</param>
        /// <returns>A string containing the mock URL</returns>
        private string GenerateMockAddress(MockSettings mockSettings, string bindingsEndpointName)
        {
            string mockAddress = null;

            if (!string.IsNullOrEmpty(mockSettings.EndpointName))
            {
                mockAddress = string.Format(
                    CultureInfo.InvariantCulture,
                    MockAddressTemplate, 
                    mockSettings.EndpointName);
            }
            else
            {
                mockAddress = string.Format(
                    CultureInfo.InvariantCulture, 
                    MockAddressTemplate, 
                    bindingsEndpointName);
            }            

            if (!string.IsNullOrEmpty(mockSettings.Operation))
            {
                mockAddress += string.Format(
                    CultureInfo.InvariantCulture, 
                    "/{0}", 
                    mockSettings.Operation);
            }

            return mockAddress;
        }

        /// <summary>
        /// Extracts the configured custom service behaviors from the transport configuration of a receive endpoint
        /// </summary>
        /// <param name="transportConfig">The string containing the transport configuration for a receive endpoint</param>
        /// <returns>A string containing the custom service behaviors if any specified</returns>
        private string ExtractCustomServiceBehaviors(string transportConfig)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "TransMock.Mockifier.Parser.ExtractCustomServiceBehaviors() called with parameter: " +
                    transportConfig);

                var transportConfigElement = this.GetCustomBehaviorConfigElement(transportConfig);

                var customBehaviorConfig = transportConfigElement
                    .Elements().Where(e => e.Name == "ServiceBehaviorConfiguration")
                    .Single();

                return customBehaviorConfig.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "TransMock.Mockifier.Parser.ExtractCustomServiceBehaviors() threw exception: " +
                    ex.Message);

                return null;
            }
        }

        /// <summary>
        /// Extracts the custom endpoint behaviors for a send endpoint
        /// </summary>
        /// <param name="transportConfig">The string containing the transport configuration</param>
        /// <returns>A string containing any custom endpoint behaviors</returns>
        private string ExtractCustomEndpointBehaviors(string transportConfig)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "TransMock.Mockifier.Parser.ExtractCustomEndpointBehaviors() called with parameter: " +
                    transportConfig);

                var transportConfigElement = this.GetCustomBehaviorConfigElement(transportConfig);

                var customBehaviorConfig = transportConfigElement
                    .Elements().Where(e => e.Name == "EndpointBehaviorConfiguration")
                    .Single();

                return customBehaviorConfig.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "TransMock.Mockifier.Parser.ExtractCustomEndpointBehaviors() threw exception: " +
                    ex.Message);

                return null;
            }
        }

        /// <summary>
        /// Gets the custom behavior configuration element from the transport configuration
        /// </summary>
        /// <param name="transportConfig">The string containing the transport configuration</param>
        /// <returns>An XML element containing the custom behavior configuration</returns>
        private XElement GetCustomBehaviorConfigElement(string transportConfig)
        {
            string unescapedTransportConfig = transportConfig
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">");

            var transportConfigElement = XElement.Parse(unescapedTransportConfig);

            return transportConfigElement;
        }
    }
}
