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
/// Module      :  MockAdapterInboundHandler.cs
/// Description :  This class implements an interface for listening or polling for data.
/// -----------------------------------------------------------------------------------------------------------
/// 
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

using Microsoft.ServiceModel;
using Microsoft.ServiceModel.Channels.Common;

using TransMock.Communication.NamedPipes;

#endregion

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// Mock adapter inbound handler class
    /// </summary>
    public class MockAdapterInboundHandler : MockAdapterHandlerBase, IInboundHandler
    {   
        /// <summary>
        /// The streaming named pipe server used for communication
        /// </summary>
        private IAsyncStreamingServer pipeServer;

        /// <summary>
        /// The internal queue where messages are put when received from an external system
        /// </summary>        
        private Queue<MessageConnectionPair> inboundQueue;
            
        /// <summary>
        /// Object used for synchronizing access to the inbound queue
        /// </summary>
        private object inboundQueueSyncLock = new object();       

        /// <summary>
        /// Holds the list of promoted properties as configured in the adapter UI
        /// </summary>
        private string propertiesToPromote;

        /// <summary>
        /// The parser for property promotion
        /// </summary>
        private AdapterPropertyParser propertyParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterInboundHandler"/> class
        /// </summary>
        /// <param name="connection">The connection object for the inbound handler</param>
        /// <param name="metadataLookup">The metadata lookup object for the handler</param>
        public MockAdapterInboundHandler(
            MockAdapterConnection connection,
            MetadataLookup metadataLookup)
            : base(connection, metadataLookup)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            this.propertiesToPromote = connection.ConnectionFactory.Adapter.PromotedProperties;
            this.propertyParser = new AdapterPropertyParser();
        }

        #region IInboundHandler Members

        /// <summary>
        /// Start the listener
        /// </summary>
        /// <param name="actions">A list of valid actions for starting the operation</param>
        /// <param name="timeout">The timeout for starting the listener</param>
        public void StartListener(string[] actions, TimeSpan timeout)
        {
            this.ParsePropertiesForPromotion();

            lock (this.inboundQueueSyncLock)
            {
                this.inboundQueue = new Queue<MessageConnectionPair>(3);
            }

            this.pipeServer = new StreamingNamedPipeServer(
                this.Connection.ConnectionFactory.ConnectionUri
                    .Uri.AbsolutePath);

            this.pipeServer.ClientConnected += this.pipeServer_ClientConnected;
            this.pipeServer.ReadCompleted += this.pipeServer_ReadCompleted;

            this.pipeServer.Start();
        }
        
        /// <summary>
        /// Stop the listener
        /// </summary>
        /// <param name="timeout">The timeout period for stopping the listener</param>
        public void StopListener(TimeSpan timeout)
        {            
            try
            {
                this.propertyParser.Clear();

                if (this.inboundQueue != null)
                {
                    lock (this.inboundQueueSyncLock)
                    {
                        System.Diagnostics.Debug.WriteLine("Cleaning up the inbound queue");

                        this.inboundQueue.Clear();
                        this.inboundQueue = null;   
                    }
                }                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StopListener threw an exception: " + ex.Message);

                throw;
            }
            finally
            {
                this.pipeServer.Stop();
            }
        }

        /// <summary>
        /// Tries to receive a message within a specified interval of time. 
        /// </summary>
        /// <param name="timeout">A timeout period within which to receive a message</param>
        /// <param name="message">The Message instance that has been received</param>
        /// <param name="reply">The reply object instance for sending reply messages</param>
        /// <returns>A boolean indicating whether a message was received during the defined timeout</returns>
        public bool TryReceive(TimeSpan timeout, out System.ServiceModel.Channels.Message message, out IInboundReply reply)
        {
            reply = null;

            message = null;
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            
            System.Diagnostics.Debug.WriteLine("Starting the loop over the internal queue");

            while (true)
            {                
                lock (this.inboundQueueSyncLock)
                {
                    if (this.inboundQueue == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Inbound queue is null");
                        
                        // listener has been closed
                        return false;
                    }

                    if (this.inboundQueue.Count != 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Inbound queue contains messages");
                        var msgHelper = this.inboundQueue.Dequeue();

                        if (msgHelper != null)
                        {
                            // Assigning the message that was received
                            message = msgHelper.Message;

                            // Creating the proper reply instance
                            reply = new MockAdapterInboundReply(
                                this.pipeServer,
                                msgHelper.ConnectionId,
                                Encoding.GetEncoding(   
                                    this.Connection.ConnectionFactory.Adapter.Encoding));                            
                               
                            System.Diagnostics.Debug.WriteLine("Message dequeued from the inbound queue");

                            return true;
                        }
                    }
                }

                if (timeoutHelper.IsExpired)
                {
                    System.Diagnostics.Debug.WriteLine("The reception timed out");

                    return false;
                }

                // Wait for sometime, and check again
                System.Threading.Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Returns a value that indicates whether a message has arrived within a specified interval of time.
        /// </summary>
        /// <param name="timeout">The time period to wait for a message</param>
        /// <returns>A boolean indicating whether a message has arrived within a specified time interval</returns>
        public bool WaitForMessage(TimeSpan timeout)
        {
            while (this.inboundQueue.Count == 0) 
            { 
            }

            MessageConnectionPair msgHelper = this.inboundQueue.Peek();

            if (msgHelper != null && msgHelper.Message != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion IInboundHandler Members

        #region Dispose implementation
        
        /// <summary>
        /// Disposes the object
        /// </summary>
        /// <param name="disposing">Indicates how the object should be disposed</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion

        #region Pipe server event handlers
        /// <summary>
        /// Event handler for the ReadCompleted event
        /// </summary>
        /// <param name="sender">The object that fired the event</param>
        /// <param name="e">The event parameters</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "Needed as per design")]
        private void pipeServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "Writing the message contents to the message body",
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");
                
                string msgContents = null;

                msgContents = string.Format(
                        CultureInfo.InvariantCulture,
                        "<MessageContent>{0}</MessageContent>",
                        e.Message.BodyBase64);

                XmlReader xr = XmlReader.Create(new StringReader(msgContents));

                Message inMsg = Message.CreateMessage(MessageVersion.Default, string.Empty, xr);

                System.Diagnostics.Debug.WriteLine(
                    "Message constructed. Promoting any properties to it",
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");

                // Add any statically configured properties in the message context
                this.propertyParser.PromoteProperties(inMsg, e.Message.Properties);

                if (inMsg != null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Enqueuing message to the internal queue",
                        "TransMock.Wcf.Adapter.MockAdapterInboundHandler");

                    lock (this.inboundQueueSyncLock)
                    {
                        // Adding the message and pipe connection to the inbound queue
                        this.inboundQueue.Enqueue(new MessageConnectionPair(inMsg, e.ConnectionId));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "ReadCompleted event handler threw an exception: " + ex.Message,
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");

                throw;
            }
        }

        /// <summary>
        /// Event handler method for the ClientConnected event
        /// </summary>
        /// <param name="sender">The object which fired the event</param>
        /// <param name="e">The event parameter</param>
        private void pipeServer_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(
                "ClientConnected event handler called with connection Id: " + e.ConnectionId,
                "TransMock.Wcf.Adapter.MockAdapterInboundHandler");
        }
        #endregion

        #region Property promotion methods
        /// <summary>
        /// Parses the properties that were provided for promotion
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Needed as per design")]
        private void ParsePropertiesForPromotion()
        {
            try
            {
                this.propertyParser.Init(this.propertiesToPromote);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ParsePropertiesForPromotion threw an exception: " + ex.Message);
            }
        } 
   
        #endregion        
    }
}
