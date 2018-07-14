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

/// -----------------------------------------------------------------------------------------------------------
/// Module      :  MockAdapterOutboundHandler.cs
/// Description :  This class is used for sending data to the target system
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

using Microsoft.ServiceModel.Channels.Common;

using TransMock.Communication.NamedPipes;

#endregion

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// the mock adapter outbound handler class. This class is used for sending data to the target system
    /// </summary>
    public class MockAdapterOutboundHandler : MockAdapterHandlerBase, IOutboundHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterOutboundHandler"/> class
        /// </summary>
        /// <param name="connection">The connection object for the outbound handler</param>
        /// <param name="metadataLookup">The metadata lookup object for the outbound handler</param>
        public MockAdapterOutboundHandler(
            MockAdapterConnection connection,
            MetadataLookup metadataLookup)
            : base(connection, metadataLookup)
        {
            
        }

        #region Private methods
        /// <summary>
        /// Copies the promoted properties from the BizTalk message to the mock message
        /// </summary>
        /// <param name="message">The message from where the properties will be copied</param>
        /// <param name="mockMessage">The mock message to where the properties will be copied</param>
        private static void CopyPromotedProperties(Message message, MockMessage mockMessage)
        {

            foreach (var property in message.Properties)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Property name:{0}, value:{1}",
                        property.Key, 
                        property.Value ?? property.Value.ToString());

                try
                {
                    // Lookup the namespace prefix
                    string[] propertyParts = property.Key.Split('#');

                    if (propertyParts.Length == 2)
                    {
                        var utilsType = typeof(Utils.BizTalkProperties.Namespaces);

                        string prefix = (string)utilsType.GetProperties().Where(
                            p => p.PropertyType == typeof(string) && p.GetValue(null).ToString() == propertyParts[0])
                            .SingleOrDefault()
                            .Name;

                        // Adding the message properties to the mock message instance
                        mockMessage.Properties.Add(
                        string.Format("{0}.{1}",
                            prefix, propertyParts[1]),
                        property.Value == null ?
                            string.Empty :
                            property.Value.ToString());
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        string.Format(
                            "Property {0} not copied to mock message. Exception: {1}",
                            property.Key, ex.Message),
                        "TransMock.Wcf.Adapter.MockAdapterOutboundHandler");
                }                            
            }            
        } 
#endregion

#region IOutboundHandler Members
        /// <summary>
        /// Executes the request message on the target system and returns a response message.
        /// If there isn’t a response, this method should return null
        /// </summary>
        /// <param name="message">The message instance that will be sent as request</param>
        /// <param name="timeout">The time period within which the message shall be sent</param>
        /// <returns>A message instance that was received in response, or null in case it was no response</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "The method returns a message instance in case a response message was expected and supplied")]
        public Message Execute(Message message, TimeSpan timeout)
        {
            System.Diagnostics.Debug.WriteLine(
                "Sending an outbound message",
                "TransMock.Wcf.Adapter.MockAdapterOutboundHandler");

            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            XmlDictionaryReader xdr = message.GetReaderAtBodyContents();

            System.Diagnostics.Debug.WriteLine("Reading the message body in base64 encoding");

            // Reading the message contents as a base64 encoded string
            if (xdr.NodeType == XmlNodeType.Element)
            {
                // In case the content is nested in an element under the Body element
                xdr.Read();
            }

            byte[] msgBuffer = xdr.ReadContentAsBase64();

            var encoding = Encoding.GetEncoding(
                this.Connection.ConnectionFactory.Adapter.Encoding);
            // Create MockMessage isntance
            var mockMessage = new MockMessage(
                msgBuffer,
                encoding);

            CopyPromotedProperties(message, mockMessage);

            using (StreamingNamedPipeClient pipeClient = new StreamingNamedPipeClient(
                Connection.ConnectionFactory.ConnectionUri.Uri))
            {
                pipeClient.Connect((int)timeout.TotalMilliseconds);

                System.Diagnostics.Debug.WriteLine(
                    "The pipe client was connected!Sending the outbound message over the pipe",
                    "TransMock.Wcf.Adapter.MockAdapterOutboundHandler");

                // pipeClient.WriteAllBytes(msgBuffer);
                pipeClient.WriteMessage(mockMessage);

                System.Diagnostics.Debug.WriteLine(
                    "Outbound message sent!",
                    "TransMock.Wcf.Adapter.MockAdapterOutboundHandler");

                // Check if the IsSolicitResponse context property is present and set to true
                bool isTwoWay = false;
                object isSolicitResponseValue;

                if (message.Properties.TryGetValue(
                    "http://schemas.microsoft.com/BizTalk/2003/system-properties#IsSolicitResponse",
                    out isSolicitResponseValue))
                {
                    isTwoWay = Convert.ToBoolean(
                        isSolicitResponseValue,
                        CultureInfo.InvariantCulture);
                }

                // Check if the CorrelationToken prometed property is present
                if (isTwoWay)
                {
                    // We are in a two-way communication scenario
                    System.Diagnostics.Debug.WriteLine(
                        "Two-way communication - reading the response message",
                        "TransMock.Wcf.Adapter.MockAdapterOutboundHandler");

                    string respContents = null;

                    // We proceed with waiting for the response                    
                    Message responseMsg = null;
                    var responseMockMessage = pipeClient.ReadMessage();

                    respContents = string.Format(
                        CultureInfo.InvariantCulture,
                        "<MessageContent>{0}</MessageContent>",
                       responseMockMessage.BodyBase64);

                    XmlReader respReader = XmlReader.Create(new StringReader(respContents));

                    System.Diagnostics.Debug.WriteLine(
                        "Constructing the response WCF Message",
                        "TransMock.Wcf.Adapter.MockAdapterOutboundHandler");

                    responseMsg = Message.CreateMessage(MessageVersion.Default, string.Empty, respReader);

                    // Check if there are returned some properties that needs to be written/promoted to the message context
                    foreach (var property in responseMockMessage.Properties)
                    {
                        if (responseMsg.Properties.ContainsKey(property.Key))
                        {
                            responseMsg.Properties[property.Key] = property.Value;
                        }
                        else
                        {
                            responseMsg.Properties.Add(
                                property.Key, property.Value);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine(
                        "Response WCF Message constructed. Returning it to BizTalk",
                        "TransMock.Wcf.Adapter.MockAdapterOutboundHandler");

                    return responseMsg;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "One way communication - returning empty response WCF Message to BizTalk",
                        "TransMock.Wcf.Adapter.MockAdapterOutboundHandler");
                    // Return empty message in one-way scenario
                    return Message.CreateMessage(MessageVersion.Default, string.Empty);
                }
            }
        }       
#endregion IOutboundHandler Members
        
        /// <summary>
        /// Overrides the default dispose behavior
        /// </summary>
        /// <param name="disposing">A boolean indicating which objects to be disposed</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}