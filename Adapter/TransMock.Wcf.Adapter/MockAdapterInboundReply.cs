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
        /// The encoding for the response message
        /// </summary>
        private Encoding encoding;

        /// <summary>
        /// The instance of the pipe server
        /// </summary>
#if NET40 || NET45 || NET451
        private IAsyncStreamingServer pipeServer;
#elif NET462 || NET48
        private IStreamingServerAsync pipeServer;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterInboundReply"/> class
        /// </summary>
        /// <param name="pipeServer">The instance of the pipe server</param>
        /// <param name="connectionId">The Id of the connection on which the reply should be sent back</param>        
        public MockAdapterInboundReply(
#if NET40 || NET45 || NET451
        IAsyncStreamingServer pipeServer,
#elif NET462 || NET48
        IStreamingServerAsync pipeServer,
#endif
            int connectionId,
            Encoding encoding)
        {
            this.pipeServer = pipeServer;
            this.connectionId = connectionId;

            if (encoding != null)
            {
                this.encoding = encoding;
            }
            else
            {
                this.encoding = Encoding.UTF8;
            }
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

                MockMessage mockMessage = PrepareMockMessage(message);

                if (mockMessage == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Empty response mock message. Exiting",
                        "TransMock.Wcf.Adapter.MockAdapterInboundHandler");

                    return;
                }
                
                System.Diagnostics.Debug.WriteLine(
                    "Writing the message properties to the response mock message",
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");                
                
                // Add the message properties to the mock message
                foreach (var property in message.Properties)
                {
                    mockMessage.Properties.Add(
                        property.Key,
                        property.Value.ToString());
                }

                System.Diagnostics.Debug.WriteLine(
                    "Writing the response mock message to the pipe",
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");

                // Write it to the pipe server
#if NET40 || NET45 || NET451
                this.pipeServer.WriteMessage(connectionId, mockMessage);
#elif NET462 || NET48
                var writeTask = this.pipeServer.WriteMessageAsync(connectionId, mockMessage);
                writeTask.Wait();
#endif

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
#if NET40 || NET45 || NET451
                this.pipeServer.Disconnect(this.connectionId);
#elif NET462 || NET48
                var disconnectTask = this.pipeServer.DisconnectAsync(this.connectionId);
                disconnectTask.Wait();
#endif
                // Disconnecting the pipe connection

            }
        }
        #endregion InboundReply Members

        private MockMessage PrepareMockMessage(System.ServiceModel.Channels.Message message)
        {
            byte[] msgBuffer = null;

            if (!message.IsFault)
            {
                // Handling regular content messages
                System.Diagnostics.Debug.WriteLine(
                    "Handling content response message",
                    "TransMock.Wcf.Adapter.MockAdapterInboundReply");

                XmlDictionaryReader xdr = message.GetReaderAtBodyContents();

                // Read the start element and extract its contents as a base64 encoded bytes                
                if (xdr.NodeType == XmlNodeType.Element)
                {
                    // in case the content is nested in an element under the Body element
                    xdr.Read();
                }

                msgBuffer = xdr.ReadContentAsBase64();                
                
            }
            else
            {
                // Handling faults returned by BizTalk
                System.Diagnostics.Debug.WriteLine(
                    "Handling fault response message",
                    "TransMock.Wcf.Adapter.MockAdapterInboundReply");
                using (var messageBuffer = message.CreateBufferedCopy(1024 ^ 3)) // Allowing for buffer of 1 GB
                { 
                    using (var msgStream = new System.IO.MemoryStream(4096))
                    {
                        messageBuffer.WriteMessage(msgStream);

                        msgBuffer = Convert.FromBase64String(
                            Convert.ToBase64String(msgStream.ToArray()));
                    }
                    
                }

            }

            if (msgBuffer.Length == 0)
            {
                // Message is with empty body, simply return
                System.Diagnostics.Debug.WriteLine(
                    "Response message has empty body. Exiting.",
                    "TransMock.Wcf.Adapter.MockAdapterInboundReply");

                return null;
            }

            // Create MockMessage intance
            var mockMessage = new MockMessage(
                msgBuffer,
                this.encoding);

            return mockMessage;
        }

    }
}
