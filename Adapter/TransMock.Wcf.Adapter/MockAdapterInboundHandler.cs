/// -----------------------------------------------------------------------------------------------------------
/// Module      :  WCFMockAdapterInboundHandler.cs
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



#endregion

namespace TransMock.Wcf.Adapter
{
    public class MockAdapterInboundHandler : MockAdapterHandlerBase, IInboundHandler
    {   
        /// <summary>
        /// Holds the reference to the async object returned upon opening the pipe
        /// </summary>
        private IAsyncResult openAsyncResult;
        /// <summary>
        /// Holds the reference to the async object returned upon beginning read operation from the pipe
        /// </summary>
        private IAsyncResult readAsyncResult;

        private Dictionary<int, NamedPipeServerStream> pipeServers;

        /// <summary>
        /// The internal queue where messages are put when received from an external system
        /// </summary>
        // Defined as static due to a wiered bug while running tests consequtevely
        private Queue<MessageConnectionPair> inboundQueue;
        /// <summary>
        /// Object used for syncronizing access to the inbound queue
        /// </summary>
        private object inboundQueueSyncLock = new object();
        private object pipeSyncLock = new object();


        /// <summary>
        /// Initializes a new instance of the WCFMockAdapterInboundHandler class
        /// </summary>
        public MockAdapterInboundHandler(MockAdapterConnection connection
            , MetadataLookup metadataLookup)
            : base(connection, metadataLookup)
        {
            
        }

        #region IInboundHandler Members

        /// <summary>
        /// Start the listener
        /// </summary>
        public void StartListener(string[] actions, TimeSpan timeout)
        {
            lock (inboundQueueSyncLock)
            {
                inboundQueue = new Queue<MessageConnectionPair>(3);
            }

            lock (pipeSyncLock)
            {
                pipeServers = new Dictionary<int, NamedPipeServerStream>();
            }

            CreatePipeServer();            
        }        

        /// <summary>
        /// Stop the listener
        /// </summary>
        public void StopListener(TimeSpan timeout)
        {            
            try
            {
                if (inboundQueue != null)
                {
                    lock (inboundQueueSyncLock)
                    {
                        System.Diagnostics.Debug.WriteLine("Cleaning up the inbound queue");

                        while (inboundQueue.Count != 0)
                        {
                            MessageConnectionPair msgHelper = inboundQueue.Dequeue();
                            try
                            {
                                if (msgHelper.PipeConnection != null && msgHelper.PipeConnection.IsConnected)
                                {
                                    //We disconnect the pipe connection
                                    System.Diagnostics.Debug.WriteLine("Disconnecting a live pipe server");
                                    msgHelper.PipeConnection.Disconnect();
                                    System.Diagnostics.Debug.WriteLine("Pipe server disconnected");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Exception thrown in StopListener: " + ex.Message);
                            }
                            finally
                            {
                                if (msgHelper.PipeConnection != null)
                                {
                                    System.Diagnostics.Debug.WriteLine("Closing a pipe server");
                                    msgHelper.PipeConnection.Close();
                                    System.Diagnostics.Debug.WriteLine("Pipe server closed");
                                }
                            }
                        }                        
                    }
                }
                
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (inboundQueue != null)
                {
                    lock (inboundQueueSyncLock)
                    {
                        System.Diagnostics.Debug.WriteLine("Clearing the inbound queue");

                        inboundQueue.Clear();
                        inboundQueue = null;
                        System.Diagnostics.Debug.WriteLine("The inbound queue was cleared");
                    }
                }

                lock (pipeSyncLock)
                {
                    System.Diagnostics.Debug.WriteLine("Clearing the pending pipe servers. Currently the number of servers is: " + pipeServers.Count);

                    foreach (var pipeServer in pipeServers.Values)
                    {
                        System.Diagnostics.Debug.WriteLine("Closing existing server");
                        //Close any existing pipeServers
                        pipeServer.Close();
                    }

                    pipeServers.Clear();
                    pipeServers = null;

                    System.Diagnostics.Debug.WriteLine("All pipe servers cleared");
                }
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
                            reply = new WCFMockAdapterInboundReply(msgHelper.PipeConnection,
                                Encoding.GetEncoding(Connection.ConnectionFactory.Adapter.Encoding));//Creating the proper reply instance
                            (reply as WCFMockAdapterInboundReply).ReplySent += new EventHandler<ReplySentEventArgs>(WCFMockAdapterInboundHandler_ReplySent);
                               
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
        /// Event handler of the ReplySent event fired from the reply object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WCFMockAdapterInboundHandler_ReplySent(object sender, ReplySentEventArgs e)
        {
            //Close the connection properly
            try
            {
                System.Diagnostics.Debug.WriteLine("Disconnecting and closing the pipe server connection");
                lock (pipeSyncLock)
                {
                    if (e.PipeServer != null && e.PipeServer.IsConnected)
                    {
                        e.PipeServer.Disconnect();
                        System.Diagnostics.Debug.WriteLine("Pipe server disconnected");
                    }
                }
                
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("Closing the pipe server connection");

                lock (pipeSyncLock)
                {
                    if (pipeServers.Keys.Contains(e.PipeServer.GetHashCode()))
                    {
                        pipeServers.Remove(e.PipeServer.GetHashCode());
                    }

                    e.PipeServer.Close();
                }               

                System.Diagnostics.Debug.WriteLine("Pipe server closed");
            }
            //Here we create a new pipe server
            CreatePipeServer();
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

        #region Pipe operation methdos
        /// <summary>
        /// Creates a pipe server instance
        /// </summary>
        private void CreatePipeServer()
        {
            try
            {
                lock (pipeSyncLock)
                {
                    //Creating the named pipe server
                    NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                        this.Connection.ConnectionFactory.ConnectionUri.Uri.AbsolutePath,
                        PipeDirection.InOut, 5, PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous, 4096, 4096);

                    pipeServers.Add(pipeServer.GetHashCode(), pipeServer);

                    //Starting the waiting for client connetions.
                    //Notice how the pipe server instance is passed as a async state object
                    openAsyncResult = pipeServer.BeginWaitForConnection(cb => PipeClientConnected(cb),
                        pipeServer);
                }              
            }
            finally
            {

            }

        }        
        /// <summary>
        /// Invoked asyncroubously when a new client connects to the pipe server
        /// </summary>
        /// <param name="ar">The async result of the operation that triggered the method</param>
        private void PipeClientConnected(IAsyncResult ar)
        {
            System.Diagnostics.Debug.WriteLine("Pipe client connected");

            var pipeConnection = (NamedPipeServerStream)ar.AsyncState;

            //lock (pipeSyncLock)
            //{                
                try
                {
                    //We first end the waiting for connection
                    pipeConnection.EndWaitForConnection(ar);                    
                }
                catch (System.ObjectDisposedException)
                {
                    System.Diagnostics.Debug.WriteLine("Pipe has been disposed!Exiting without further processing");
                    return;
                }

                byte[] inBuffer = new byte[pipeConnection.InBufferSize];

                //Starting async read by passing the named pipe conenction as a async state parameter again.
                readAsyncResult = pipeConnection.BeginRead(inBuffer, 0,
                    inBuffer.Length, cb => PipeReadAsync(cb),
                    new AsyncReadState
                    {
                        PipeConnection = pipeConnection,
                        InStream = new MemoryStream(pipeConnection.InBufferSize),
                        RawData = inBuffer
                    });
            //}
        }
        /// <summary>
        /// Asyncrounous callback for a read operation from the pipe
        /// </summary>
        /// <param name="ar">The async result instanced passed to the method</param>
        private void PipeReadAsync(IAsyncResult ar)
        {
            System.Diagnostics.Debug.WriteLine("Beginning reading from the pipe");
            //Extracting the pipe connection from which the data is being read
            var state = ar.AsyncState as AsyncReadState;

            //lock (pipeSyncLock)
            //{
                int bytesRead = state.PipeConnection.EndRead(ar);
                
                state.InStream.Write(state.RawData, 0, bytesRead);

                if (!state.PipeConnection.IsMessageComplete)
                    readAsyncResult = state.PipeConnection.BeginRead(state.RawData, 0,
                        state.RawData.Length, cb => PipeReadAsync(cb), state);
                else
                {
                    System.Diagnostics.Debug.WriteLine("Message was read from the pipe");
                    //Writing the mem stream contents to a WCF Message object                                      
                    Message inMsg = null;
                    
                    System.Diagnostics.Debug.WriteLine(string.Format("Writing the message contents to the message body"));
                    //Adding the message contents to a predefined XML structure
                    string msgContents = string.Format("<MessageContent>{0}</MessageContent>",
                        Convert.ToBase64String(state.InStream.ToArray()));

                    XmlReader xr = XmlReader.Create(new StringReader(msgContents));

                    inMsg = Message.CreateMessage(MessageVersion.Default, string.Empty, xr);
                    
                    
                    if (inMsg != null)
                    {
                        lock (inboundQueueSyncLock)
                        {
                            //Adding the message and pipe connection to the inbound queue
                            inboundQueue.Enqueue(new MessageConnectionPair(inMsg, state.PipeConnection));
                        }
                    }

                    //state = null;
                }
	        //}
        }

        #endregion
    }

    internal class WCFMockAdapterInboundReply : InboundReply
    {
        private NamedPipeServerStream pipeServer;
        private Encoding encoding;

        public WCFMockAdapterInboundReply(NamedPipeServerStream pipeServer, Encoding encoding)
        {
            this.pipeServer = pipeServer;
            this.encoding = encoding;
        }

        public event EventHandler<ReplySentEventArgs> ReplySent;

        protected void OnReplySent(NamedPipeServerStream pipeServer)
        {
            if (ReplySent != null)
            {
                ReplySent(this, new ReplySentEventArgs(pipeServer));                
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

                System.Diagnostics.Debug.WriteLine("Writing the response message to the pipe");
                //Write it to the pipe                

                pipeServer.Write(msgBuffer, 0, msgBuffer.Length);
                pipeServer.Flush();

                System.Diagnostics.Debug.WriteLine("The response message was sent to the client");

                pipeServer.WaitForPipeDrain();

                System.Diagnostics.Debug.WriteLine("The response message was read by the client");                

            }
            catch (XmlException xex)//Thrown when a message with empty body is sent as reply, in the case of one way communication
            {
                System.Diagnostics.Debug.WriteLine(string.Format("XML exception thrown upon sending response: {0}", xex.Message));
            }
            catch (ObjectDisposedException odex)//Thrown when the pipe has been already disposed
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Disposed exception thrown upon sending response: {0}", odex.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("General exception thrown upon sending response: {0}", ex.Message));

                throw ex;
            }
            finally
            {
                OnReplySent(pipeServer);
            }

        }


        #endregion InboundReply Members
    }

    internal class MessageConnectionPair
    {
        public MessageConnectionPair(Message message, NamedPipeServerStream pipeConnection)
        {            
            Message = message;
            PipeConnection = pipeConnection;
        }

        public Message Message { get; private set; }

        public NamedPipeServerStream PipeConnection { get; private set; }
    }

    internal class ReplySentEventArgs : EventArgs
    {
        public ReplySentEventArgs(NamedPipeServerStream pipeServer)
        {
            PipeServer = pipeServer;
        }

        public NamedPipeServerStream PipeServer { get; private set; }
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
