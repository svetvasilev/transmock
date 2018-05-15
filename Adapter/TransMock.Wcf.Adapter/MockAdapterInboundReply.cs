using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.ServiceModel.Channels.Common;

using TransMock.Communication.NamedPipes;

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// This class implements the logic for sending a reply to the caller
    /// </summary>
    internal class MockAdapterInboundReply : InboundReply
    {
        /// <summary>
        /// The connection id for the response connection
        /// </summary>
        private int connectionId;

        /// <summary>
        /// The instance of the pipe server
        /// </summary>
        private IAsyncStreamingServer pipeServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterInboundReply"/> class
        /// </summary>
        /// <param name="pipeServer">The instance of the pipe server</param>
        /// <param name="connectionId">The Id of the connection on which the reply should be sent back</param>        
        public MockAdapterInboundReply(
            IAsyncStreamingServer pipeServer,
            int connectionId)
        {
            this.pipeServer = pipeServer;
            this.connectionId = connectionId;
        }

        #region InboundReply Members

        /// <summary>
        /// Abort the inbound reply call
        /// </summary>
        public override void Abort()
        {
        }

        /// <summary>
        /// Reply message implemented
        /// </summary>
        /// <param name="message">The response message to be sent</param>
        /// <param name="timeout">The time period within which to attempt to send the response message</param>
        public override void Reply(System.ServiceModel.Channels.Message message, TimeSpan timeout)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Sending a response message");

                if (message == null)
                {
                    throw new ArgumentNullException("message");
                }

                if (message.IsEmpty)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Response message is empty. Exiting.",
                        "TransMock.Wcf.Adapter.MockAdapterInboundReply");

                    return;
                }

                XmlDictionaryReader xdr = message.GetReaderAtBodyContents();

                // Read the start element and extract its contents as a base64 encoded bytes                
                if (xdr.NodeType == XmlNodeType.Element)
                {
                    // in case the content is nested in an element under the Body element
                    xdr.Read();
                }

                byte[] msgBuffer = xdr.ReadContentAsBase64();

                if (msgBuffer.Length == 0)
                {
                    // Message is with empty body, simply return
                    System.Diagnostics.Debug.WriteLine(
                        "Response message has empty body. Exiting.",
                        "TransMock.Wcf.Adapter.MockAdapterInboundReply");

                    return;
                }

                System.Diagnostics.Debug.WriteLine(
                    "Writing the response message to the pipe",
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");

                // Ccreate MockMessage isntance
                var mockMessage = new MockMessage();

                // Assign the message body
                mockMessage.Body = Convert.ToBase64String(msgBuffer);

                // Add the message properties to the mock message
                foreach (var property in message.Properties)
                {
                    mockMessage.Properties.Add(
                        property.Key,
                        property.Value.ToString());
                }
                // Write it to the pipe server
                //this.pipeServer.WriteAllBytes(this.connectionId, msgBuffer);
                this.pipeServer.WriteMessage(connectionId, mockMessage);

                System.Diagnostics.Debug.WriteLine(
                    "The response message was sent to the client",
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        "General exception thrown upon sending response: {0}",
                        ex.Message));

                throw;
            }
            finally
            {
                // Disconnecting the pipe connection
                this.pipeServer.Disconnect(this.connectionId);
            }
        }
        #endregion InboundReply Members
    }
}
