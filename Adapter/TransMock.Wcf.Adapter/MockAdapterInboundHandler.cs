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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.ServiceModel.Channels;
using System.Xml;

using Microsoft.ServiceModel.Channels.Common;
using Microsoft.ServiceModel;

using TransMock.Communication.NamedPipes;

#endregion

namespace TransMock.Wcf.Adapter
{
    public class MockAdapterInboundHandler : MockAdapterHandlerBase, IInboundHandler
    {   
        /// <summary>
        /// The streaming named pipe server used for communication
        /// </summary>
        private StreamingNamedPipeServer pipeServer;

        /// <summary>
        /// The internal queue where messages are put when received from an external system
        /// </summary>
        // Defined as static due to a wiered bug while running tests consequtevely
        private Queue<MessageConnectionPair> inboundQueue;
        /// <summary>
        /// Object used for syncronizing access to the inbound queue
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
        /// Initializes a new instance of the WCFMockAdapterInboundHandler class
        /// </summary>
        public MockAdapterInboundHandler(MockAdapterConnection connection
            , MetadataLookup metadataLookup)
            : base(connection, metadataLookup)
        {
            propertiesToPromote = connection.ConnectionFactory.Adapter.PromotedProperties;
            propertyParser = new AdapterPropertyParser();
        }

        #region IInboundHandler Members

        /// <summary>
        /// Start the listener
        /// </summary>
        public void StartListener(string[] actions, TimeSpan timeout)
        {
            ParsePropertiesForPromotion();

            lock (inboundQueueSyncLock)
            {
                inboundQueue = new Queue<MessageConnectionPair>(3);
            }

            pipeServer = new StreamingNamedPipeServer(
                this.Connection.ConnectionFactory.ConnectionUri
                    .Uri.AbsolutePath);

            pipeServer.ClientConnected += pipeServer_ClientConnected;
            pipeServer.ReadCompleted += pipeServer_ReadCompleted;
            pipeServer.Start();
        }
        
        /// <summary>
        /// Stop the listener
        /// </summary>
        public void StopListener(TimeSpan timeout)
        {            
            try
            {
                propertyParser.Clear();

                if (inboundQueue != null)
                {
                    lock (inboundQueueSyncLock)
                    {
                        System.Diagnostics.Debug.WriteLine("Cleaning up the inbound queue");

                        inboundQueue.Clear();
                        inboundQueue = null;   
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
                pipeServer.Stop();
            }
        }

        /// <summary>
        /// Tries to receive a message within a specified interval of time. 
        /// </summary>
        public bool TryReceive(TimeSpan timeout, out System.ServiceModel.Channels.Message message, out IInboundReply reply)
        {
            reply = null;

            message = null;
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            
            System.Diagnostics.Debug.WriteLine("Starting the loop over the internal queue");

            while (true)
            {                
                lock (inboundQueueSyncLock)
                {
                    if (inboundQueue == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Inbound queue is null");
                        //listener has been closed
                        return false;
                    }
                    if (inboundQueue.Count != 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Inbound queue contains messages");
                        var msgHelper = inboundQueue.Dequeue();

                        if (msgHelper != null)
                        {
                            message = msgHelper.Message;//Assigning the message that was received
                            reply = new WCFMockAdapterInboundReply(pipeServer,
                                msgHelper.ConnectionId,
                                Encoding.GetEncoding(Connection.ConnectionFactory.Adapter.Encoding));//Creating the proper reply instance                            
                               
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
                //wait for sometime, and check again
                System.Threading.Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Returns a value that indicates whether a message has arrived within a specified interval of time.
        /// </summary>
        public bool WaitForMessage(TimeSpan timeout)
        {
            while (inboundQueue.Count == 0) { };
            MessageConnectionPair msgHelper = inboundQueue.Peek();
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

        #region Pipe server event handlers
        private void pipeServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Writing the message contents to the message body",
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");
                
                string msgContents = null;
                //Adding the message contents to a predefined XML structure
                //TODO: refactor to a more efficien implementation
                using(BinaryReader br = new BinaryReader(e.MessageStream))
	            {
                    msgContents = string.Format("<MessageContent>{0}</MessageContent>",
                        Convert.ToBase64String(br.ReadBytes((int)br.BaseStream.Length)));
	            }
                

                XmlReader xr = XmlReader.Create(new StringReader(msgContents));

                Message inMsg = Message.CreateMessage(MessageVersion.Default, string.Empty, xr);

                System.Diagnostics.Debug.WriteLine("Message constructed. Promoting any properties to it",
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");

                //Add any configured properties in the message context
                propertyParser.PromoteProperties(inMsg);                    
                    
                if (inMsg != null)
                {
                    System.Diagnostics.Debug.WriteLine("Enqueuing message to the internal queue",
                        "TransMock.Wcf.Adapter.MockAdapterInboundHandler");

                    lock (inboundQueueSyncLock)
                    {
                        //Adding the message and pipe connection to the inbound queue
                        inboundQueue.Enqueue(new MessageConnectionPair(inMsg, e.ConnectionId));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ReadCompleted event handler threw an exception: " + ex.Message,
                        "TransMock.Wcf.Adapter.MockAdapterInboundHandler");
                throw;
            }
        }

        private void pipeServer_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ClientConnected event handler called with connection Id: " + e.ConnectionId,
                        "TransMock.Wcf.Adapter.MockAdapterInboundHandler");
        }
        #endregion

        #region Property promotion methods
        private void ParsePropertiesForPromotion()
        {
            try
            {
                propertyParser.Init(propertiesToPromote);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ParsePropertiesForPromotion threw an exception: " + ex.Message);
            }
        }    
        #endregion
    }

    /// <summary>
    /// This class implements the logic for sending a reply to the caller
    /// </summary>
    internal class WCFMockAdapterInboundReply : InboundReply
    {
        private int connectionId;
        private StreamingNamedPipeServer pipeServer;
        private Encoding encoding;

        /// <summary>
        /// Creates a new instance of the class
        /// </summary>
        /// <param name="pipeServer">The instance of the pipe server</param>
        /// <param name="connectionId">The Id of the connection on which the reply should be sent back</param>
        /// <param name="encoding">The encoding of the reply</param>
        public WCFMockAdapterInboundReply(
            StreamingNamedPipeServer pipeServer, 
            int connectionId,
            Encoding encoding)
        {            
            this.pipeServer = pipeServer;
            this.connectionId = connectionId;
            this.encoding = encoding;
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
        public override void Reply(System.ServiceModel.Channels.Message message
            , TimeSpan timeout)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Sending a response message");
                XmlDictionaryReader xdr = message.GetReaderAtBodyContents();
                //Read the start element and extract its contents as a base64 encoded bytes                
                if (xdr.NodeType == XmlNodeType.Element)
                {
                    //in case the content is nested in an element under the Body element
                    xdr.Read();
                }

                byte[] msgBuffer = xdr.ReadContentAsBase64();

                System.Diagnostics.Debug.WriteLine("Writing the response message to the pipe",
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");
                //Write it to the pipe server
                pipeServer.WriteAllBytes(connectionId, msgBuffer);                

                System.Diagnostics.Debug.WriteLine("The response message was sent to the client",
                    "TransMock.Wcf.Adapter.MockAdapterInboundHandler");                              
            }            
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format("General exception thrown upon sending response: {0}", 
                    ex.Message));

                throw;
            }
            finally
            {
                //Disconnecting the pipe connection
                pipeServer.Disconnect(connectionId);
            }

        }
        #endregion InboundReply Members
    }

    internal class MessageConnectionPair
    {
        public MessageConnectionPair(Message message, int connectionId)
        {            
            Message = message;
            ConnectionId = connectionId;
        }

        public Message Message { get; private set; }

        public int ConnectionId { get; private set; }
    }    

    internal class AsyncReadState
    {
        public AsyncReadState()
        {

        }

        public NamedPipeServerStream PipeConnection { get; set; }

        public MemoryStream InStream { get; set; }

        public byte[] RawData { get; set; }
    }

    /// <summary>
    /// Utility class containing helper functions for measuring timeout 
    /// </summary>
    internal class TimeoutHelper
    {
        private TimeSpan timeout;
        private DateTime creationTime;
        private Boolean isInfinite;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeout"></param>
        public TimeoutHelper(TimeSpan timeout)
        {
            this.creationTime = DateTime.Now;
            this.timeout = timeout;
            if (timeout.Equals(Infinite)) this.isInfinite = true;
        }

        /// <summary>
        /// Value of infinite timespan
        /// </summary>
        public static TimeSpan Infinite
        {
            get { return TimeSpan.MaxValue; }
        }

        /// <summary>
        /// Value indicating remaining timeout
        /// </summary>
        public TimeSpan RemainingTimeout
        {
            get
            {
                if (this.isInfinite) return Infinite;
                return this.timeout.Subtract(DateTime.Now.Subtract(this.creationTime));
            }
        }

        /// <summary>
        /// Get remaining timeout value and throw an exception if the timeout
        /// has expired.
        /// </summary>
        /// <param name="exceptionMessage"></param>
        /// <returns></returns>
        public TimeSpan GetRemainingTimeoutAndThrowIfExpired(String exceptionMessage)
        {
            if (this.isInfinite) return Infinite;
            if (RemainingTimeout < TimeSpan.Zero)
            {
                throw new TimeoutException(exceptionMessage);
            }
            return RemainingTimeout;
        }

        /// <summary>
        /// Throw an exception if the timeout has expired.
        /// </summary>
        /// <param name="exceptionMessage"></param>
        public void ThrowIfTimeoutExpired(String exceptionMessage)
        {
            if (RemainingTimeout < TimeSpan.Zero)
            {
                throw new TimeoutException(exceptionMessage);
            }

        }

        /// <summary>
        /// Value indicating whether timeout has expired.
        /// </summary>
        public Boolean IsExpired
        {
            get
            {
                if (this.isInfinite) return false;
                return RemainingTimeout < TimeSpan.Zero;
            }
        }
    }

}
