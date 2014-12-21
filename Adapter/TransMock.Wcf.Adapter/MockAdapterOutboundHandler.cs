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
using System.Text;
using System.ServiceModel.Channels;
using System.IO;
using System.IO.Pipes;
using System.Xml;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    public class MockAdapterOutboundHandler : MockAdapterHandlerBase, IOutboundHandler
    {
        //private NamedPipeClientStream pipeClient;
        /// <summary>
        /// Initializes a new instance of the WCFMockAdapterOutboundHandler class
        /// </summary>
        public MockAdapterOutboundHandler(MockAdapterConnection connection
            , MetadataLookup metadataLookup)
            : base(connection, metadataLookup)
        {
        }

        #region IOutboundHandler Members

        /// <summary>
        /// Executes the request message on the target system and returns a response message.
        /// If there isn’t a response, this method should return null
        /// </summary>
        public Message Execute(Message message, TimeSpan timeout)
        {
            System.Diagnostics.Debug.WriteLine("Sending an outbound message");
            
            XmlDictionaryReader xdr = message.GetReaderAtBodyContents();
            
            System.Diagnostics.Debug.WriteLine("Reading the message body in base64 encoding");
            //Reading the message contents as a base64 encoded string
            if (xdr.NodeType == XmlNodeType.Element)//in case the content is nested in an element under the Body element
                xdr.Read();

            byte[] msgBuffer = xdr.ReadContentAsBase64();
            
            string host = Connection.ConnectionFactory.ConnectionUri.Uri.Host;
            //The pipe name is the absolute URI without the starting /
            string pipeName = Connection.ConnectionFactory.ConnectionUri.Uri.AbsolutePath.Substring(1);

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(host,
                pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {   
                pipeClient.Connect((int)timeout.TotalMilliseconds);
                //Setting the pipe read mode to message
                pipeClient.ReadMode = PipeTransmissionMode.Message;

                System.Diagnostics.Debug.WriteLine("The pipe client was connected! Sending the outbound message over the pipe");

                pipeClient.Write(msgBuffer, 0, msgBuffer.Length);
                pipeClient.Flush();

                System.Diagnostics.Debug.WriteLine("Outbound message sent!");

                pipeClient.WaitForPipeDrain();

                System.Diagnostics.Debug.WriteLine("Outbound message delivered!");
                //Check if the IsSolicitResponse context property is present and set to true
                bool isTwoWay = false;
                object isSolicitResponseValue;

                if (message.Properties.TryGetValue(
                    "http://schemas.microsoft.com/BizTalk/2003/system-properties#IsSolicitResponse",
                    out isSolicitResponseValue))
                {
                    isTwoWay = Convert.ToBoolean(isSolicitResponseValue);                 
                }

                if (isTwoWay)
                {
                    //We are in a two-way communication scenario
                    System.Diagnostics.Debug.WriteLine("Two-way communication - reading the response message");
                    
                    MemoryStream memStream = new MemoryStream(msgBuffer.Length);
                    //We proceed with waiting for the response
                    do
                    {
                        //Todo: Perform check on whether the time for this operation has elapsed
                        int byteCount = pipeClient.Read(msgBuffer, 0, msgBuffer.Length);
                        memStream.Write(msgBuffer, 0, byteCount);
                    }
                    while (!pipeClient.IsMessageComplete);
                    
                    //We rewind the stream to the beginning
                    memStream.Seek(0, SeekOrigin.Begin);

                    //Constructing the message response with a predefined XML structure
                    string respContents = string.Format("<MessageContent>{0}</MessageContent>",
                        Convert.ToBase64String(memStream.ToArray()));

                    XmlReader xrResponse = XmlReader.Create(new StringReader(respContents));

                    System.Diagnostics.Debug.WriteLine("Constructing the response WCF Message");

                    Message responseMsg = Message.CreateMessage(MessageVersion.Default, string.Empty, xrResponse);

                    System.Diagnostics.Debug.WriteLine("Response WCF Message constructed");

                    return responseMsg;
                }
                else
                    //Return empty message in one-way scenario
                    return Message.CreateMessage(MessageVersion.Default, string.Empty);
                
            }
            
        }

        #endregion IOutboundHandler Members
    }
}
