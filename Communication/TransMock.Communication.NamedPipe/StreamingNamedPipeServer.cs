/***************************************
//   Copyright 2015 - Svetoslav Vasilev

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
/// Module      :  StreamingNamedPipeClient.cs
/// Description : This class implements the logic of a streaming named pipe server
/// 
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// This class implements the logic of a streaming named pipe server
    /// </summary>
    public class StreamingNamedPipeServer : IAsyncStreamingServer
    {
        /// <summary>
        /// A dictionary containing all the active client connections
        /// </summary>
        protected Dictionary<int, NamedPipeServerStream> pipeServerConnections;
        
        /// <summary>
        /// Object used for synchronizing access to the pipe server instance
        /// </summary>
        protected object pipeSyncLock;        

        /// <summary>
        /// A manual reset event used for thread synchronization
        /// </summary>
        private ManualResetEventSlim serverStopEvent;

        /// <summary>
        /// The current number of active pipe connections
        /// </summary>
        private int activePipeConnectionCount = 0;      

        /// <summary>
        /// The current state of the server at any given moment
        /// </summary>
        private ServerState serverState = ServerState.Undefined;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingNamedPipeServer"/> class
        /// </summary>
        public StreamingNamedPipeServer()
        {
            this.pipeSyncLock = new object();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingNamedPipeServer"/> class that can be reached on the provided URL
        /// </summary>
        /// <param name="url">The URL on which the server will be reached</param>
        public StreamingNamedPipeServer(string url) : this()
        {
            this.URL = url;
        }

        #region IStreamingServer events
        /// <summary>
        /// The event fired when a client connects to the server endpoint
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// The event fired when reading data from the client completed
        /// </summary>
        public event EventHandler<AsyncReadEventArgs> ReadCompleted;
        #endregion

        /// <summary>
        /// Gets or sets the URL of the server endpoint
        /// </summary>
        public string URL 
        { 
            get; set; 
        }

        #region IStreamingServerImplementation

        /// <summary>
        /// Starts the server so that the endpoint is able to receive connection requests
        /// </summary>
        /// <returns>A boolean representing whether the server started successfully or not</returns>
        public bool Start()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "Start called", 
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                this.serverState = ServerState.Starting;

                lock (this.pipeSyncLock)
                {
                    this.pipeServerConnections = new Dictionary<int, NamedPipeServerStream>(3);
                }
                
                this.CreatePipeServer();
                
                this.serverState = ServerState.Started;
                
                System.Diagnostics.Trace.WriteLine(
                    "Named pipe server started", 
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "Start failed with exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                this.serverState = ServerState.Stopped;

                return false;
            }            
        }

        /// <summary>
        /// Stops the server which makes the endpoint unavailable for new connections
        /// </summary>
        public void Stop()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "Stop called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                this.serverState = ServerState.Stopping;

                lock (this.pipeSyncLock)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Clearing the pending pipe servers. Currently the number of servers is: " + this.pipeServerConnections.Count,
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                    foreach (var pipeServer in this.pipeServerConnections.Values)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "Closing existing server",
                            "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                        // Close any existing pipeServers
                        pipeServer.Dispose();
                    }                    

                    System.Diagnostics.Debug.WriteLine(
                        "All pipe servers closed",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                }

                System.Diagnostics.Trace.WriteLine(
                    "Stop succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "Stop failed with exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                
                throw;
            }
            finally
            {
                this.WaitForPipeServersCleanup();

                System.Diagnostics.Debug.WriteLine(
                    "Clearing the server collection",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                lock (this.pipeSyncLock)
                {
                    this.pipeServerConnections.Clear();
                    this.pipeServerConnections = null;
                }

                this.serverState = ServerState.Stopped;

                System.Diagnostics.Debug.WriteLine(
                    "Server collection cleared",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
        }

        /// <summary>
        /// Disconnects a client connection with the provided connection Id
        /// </summary>
        /// <param name="connectionId">The Id of the client connection that will be disconnected</param>
        public void Disconnect(int connectionId) 
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "Disconnect() called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                NamedPipeServerStream pipeConnection;

                lock (this.pipeSyncLock)
                {
                    try
                    {
                        pipeConnection = this.pipeServerConnections[connectionId];

                        if (pipeConnection.IsConnected)
                        {
                            pipeConnection.Disconnect();
                        }

                        // Closing the connection and disposing any resources it uses
                        pipeConnection.Dispose();
                    }
                    finally
                    {
                        this.pipeServerConnections.Remove(connectionId);
                    }                    
                }

                System.Diagnostics.Trace.WriteLine(
                    "Disconnect() succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "Disconnect() threw an exception:" + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                
                throw;
            }            
        }        

        /// <summary>
        /// Writes all bytes to the connection with the provided Id
        /// </summary>
        /// <param name="connectionId">The Id of the connection to write the data to</param>
        /// <param name="data">The data to be written represented as a byte array</param>
        public void WriteAllBytes(int connectionId, byte[] data)
        {
            System.Diagnostics.Trace.WriteLine(
                "WriteAllBytes() called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

            using (MemoryStream msgStream = new MemoryStream(data))
            {
                System.Diagnostics.Debug.WriteLine(
                    "Constructed MemoryStream of the message data.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                this.WriteStream(connectionId, msgStream);
            }

            System.Diagnostics.Trace.WriteLine(
                "WriteAllBytes() succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
        }

        /// <summary>
        /// Writes the contents of the supplied stream to a connection with the provided Id
        /// </summary>
        /// <param name="connectionId">The Id of the connection where the data shall be written to</param>
        /// <param name="data">The data to be written to the connection represented as a stream</param>
        public void WriteStream(int connectionId, Stream data)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "WriteStream() called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                NamedPipeServerStream pipeConnection;
                
                lock (this.pipeSyncLock)
                {
                    pipeConnection = this.pipeServerConnections[connectionId];                    
                }

                int byteReadCount; 
                byte[] outBuffer = new byte[4096];

                System.Diagnostics.Debug.WriteLine(
                    "Writing the message to the client.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                while ((byteReadCount = data.Read(outBuffer, 0, outBuffer.Length)) > 0)
                {
                    pipeConnection.Write(outBuffer, 0, byteReadCount);
                }

                System.Diagnostics.Debug.WriteLine(
                    "Writing the EOF byte.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                
                // Write the EOF bite
                pipeConnection.WriteByte(0x00);

                System.Diagnostics.Debug.WriteLine(
                    "Message sent to the client.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                pipeConnection.WaitForPipeDrain();

                System.Diagnostics.Trace.WriteLine(
                    "Message read by the client. WriteStream() succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "WriteStream() trhew an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                throw;
            }
        }        
        #endregion

        #region IDisposable implementation
        /// <summary>
        /// Disposes the pipe server
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion

        /// <summary>
        /// Implements specific disposal logic
        /// </summary>
        /// <param name="disposeAll">Indicates whether to dispose all or only managed objects</param>
        protected virtual void Dispose(bool disposeAll)
        {
            System.Diagnostics.Trace.WriteLine(
                "Dispose() called",
                "TransMock.Communication.NamedPipes.StreamingNamedPipeServer");

            this.serverStopEvent.Dispose();
            
            this.pipeSyncLock = null;
            this.pipeServerConnections = null;

            System.Diagnostics.Trace.WriteLine(
                "Dispose() succeeded",
                "TransMock.Communication.NamedPipes.StreamingNamedPipeServer");
        }

        /// <summary>
        /// Invokes the ClientConnected event for the connection with the given Id
        /// </summary>
        /// <param name="connectionId">The Id of the client connection that has just been established</param>
        protected void OnClientConnected(int connectionId)
        {
            if (this.ClientConnected != null)
            {
                ClientConnectedEventArgs args = new ClientConnectedEventArgs()
                {
                    ConnectionId = connectionId
                };

                this.ClientConnected(this, args);
            }
        }

        /// <summary>
        /// Fires the ReadCompleted event for the connection with the given Id
        /// </summary>
        /// <param name="connectionId">The connection Id on which the read operation completed</param>
        /// <param name="memoryStream">The data read represented as a MemoryStream object</param>
        protected void OnReadCompleted(int connectionId, MemoryStream memoryStream)
        {
            if (this.ReadCompleted != null)
            {
                AsyncReadEventArgs args = new AsyncReadEventArgs()
                {
                    ConnectionId = connectionId,
                    MessageStream = memoryStream
                };

                // Fire the event
                this.ReadCompleted(this, args);
            }
        }

        /// <summary>
        /// Creates an instance of the pipe server
        /// </summary>
        private void CreatePipeServer()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "CreatePipeServer() called",
                       "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                // Setting up pipe security
                PipeSecurity ps = new PipeSecurity();

                ps.AddAccessRule(
                    new PipeAccessRule(
                        "Users",
                        PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite,
                        System.Security.AccessControl.AccessControlType.Allow));

                System.Diagnostics.Trace.WriteLine(
                    string.Format("Creating named pipe server with URL: {0}", this.URL),
                       "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                
                // Creating the named pipe server
                NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                    this.URL,
                    PipeDirection.InOut, 
                    5, 
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous, 
                    4096, 
                    4096, 
                    ps);

                lock (this.pipeSyncLock)
                {
                    this.pipeServerConnections.Add(pipeServer.GetHashCode(), pipeServer);

                    this.activePipeConnectionCount++;
                }

                // Starting the waiting for client connetions.
                // Notice how the pipe server instance is passed as a async state object
                pipeServer.BeginWaitForConnection(
                    cb => this.PipeClientConnected(cb),
                    pipeServer.GetHashCode());

                System.Diagnostics.Trace.WriteLine(
                    "CreatePipeServer() succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "CreatePipeServer() thre an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                throw;
            }
        }

        /// <summary>
        /// Invoked asynchronously when a new client connects to the pipe server
        /// </summary>
        /// <param name="ar">The async result of the operation that triggered the method</param>
        private void PipeClientConnected(IAsyncResult ar)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "Pipe client connected",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                int pipeConnectionId = (int)ar.AsyncState;

                NamedPipeServerStream pipeConnection;

                // Extract the pipe server connection from the dictionary
                lock (this.pipeSyncLock)
                {
                    pipeConnection = this.pipeServerConnections[pipeConnectionId];

                    this.activePipeConnectionCount--;
                }

                try
                {
                    // We first end the waiting for connection
                    pipeConnection.EndWaitForConnection(ar);
                }
                catch (System.ObjectDisposedException)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Pipe has been disposed!Exiting without further processing",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                    return;
                }

                byte[] inBuffer = new byte[4096];

                // Starting async read by passing the named pipe conenction as a async state parameter again.
                pipeConnection.BeginRead(
                    inBuffer, 
                    0,
                    inBuffer.Length, 
                    cb => this.PipeReadAsync(cb),
                    new AsyncReadState
                    {
                        ConnectionId = pipeConnectionId,
                        InStream = new MemoryStream(4096),
                        RawData = inBuffer
                    });

                this.OnClientConnected(pipeConnectionId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "PipeClientConnected() threw an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
            finally
            {
                switch (this.serverState)
                {   
                    case ServerState.Started:
                        this.CreatePipeServer();
                        break;
                    case ServerState.Stopping:
                        this.SignalPipeClientDisconnect();
                        break;                    
                    default:
                        break;
                }                
            }            
        }

        /// <summary>
        /// Asynchronous callback for a read operation from the pipe
        /// </summary>
        /// <param name="ar">The async result instanced passed to the method</param>
        private void PipeReadAsync(IAsyncResult ar)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "Beginning reading from the pipe",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                // Extracting the pipe connection from which the data is being read
                var state = ar.AsyncState as AsyncReadState;
                NamedPipeServerStream pipeConnection;                
                
                lock (this.pipeSyncLock)
                {
                    pipeConnection = this.pipeServerConnections[state.ConnectionId];
                }

                int bytesRead = pipeConnection.EndRead(ar);
                bool eofReached = false;

                System.Diagnostics.Debug.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Read {0} bytes", 
                        bytesRead),
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                if (bytesRead > 0)
                {
                    eofReached = NamedPipeMessageUtils.IsEndOfMessage(state.RawData, bytesRead);
                }
                else
                {
                    eofReached = true;
                }

                state.InStream.Write(
                    state.RawData, 
                    0,
                    eofReached && bytesRead > 0 ? bytesRead - 1 : bytesRead);

                    System.Diagnostics.Debug.WriteLine(
                        string.Format(
                            CultureInfo.InvariantCulture, 
                            "Written {0} bytes to the internal stream",
                            eofReached ? bytesRead - 1 : bytesRead));                

                if (!eofReached)
                {
                    // Not EOF, continue reading
                   pipeConnection.BeginRead(
                       state.RawData, 
                       0,
                       state.RawData.Length, 
                       cb => this.PipeReadAsync(cb), 
                       state);
                }
                else
                {
                    // EOF reached, constructing the message
                    System.Diagnostics.Debug.WriteLine(
                        "Message was read from the pipe",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                    // Rewind the stream
                    state.InStream.Seek(0, SeekOrigin.Begin);

                    // Notify that the read was complete
                    this.OnReadCompleted(state.ConnectionId, state.InStream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "PipeReadAsync threw an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }            
        }        

        /// <summary>
        /// Waits for the named pipe server stream instances to clean-up
        /// </summary>
        private void WaitForPipeServersCleanup() 
        {           
            System.Diagnostics.Debug.WriteLine(
                "WaitForPipeServersCleanup called with pipeServerConnectCount: " + this.activePipeConnectionCount,
                "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

            this.serverStopEvent = new ManualResetEventSlim(false);
            this.serverStopEvent.Wait(TimeSpan.FromSeconds(10));

            System.Diagnostics.Debug.WriteLine(
                "All waiting connections stopped",
                "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

            this.activePipeConnectionCount = 0;                        
        }

        /// <summary>
        /// Signals when a pipe client disconnects
        /// </summary>
        private void SignalPipeClientDisconnect()
        {
            if (this.serverStopEvent == null)
            {
                return;
            }

            if (this.activePipeConnectionCount == 0)
            {
                // When we have no more active pipe connections we set the event
                this.serverStopEvent.Set();
            }
        }
    }   

    /// <summary>
    /// Helper class for keeping the relation between the connection and the data that is read from it
    /// </summary>
    internal class AsyncReadState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncReadState"/> class
        /// </summary>
        public AsyncReadState()
        {
        }

        /// <summary>
        /// Gets or sets the Id of the connection from which the data was read
        /// </summary>
        public int ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the data represented as a memory stream
        /// </summary>
        public MemoryStream InStream { get; set; }

        /// <summary>
        /// Gets or sets the raw data read as array of bytes
        /// </summary>
        public byte[] RawData { get; set; }
    }

    /// <summary>
    /// Enumerates the different server states
    /// </summary>
    internal enum ServerState
    {
        /// <summary>
        /// The server is in Undefined state. 
        /// This is usually the state right after a new instance of the server is instantiated
        /// </summary>
        Undefined,

        /// <summary>
        /// The server is starting
        /// </summary>
        Starting,

        /// <summary>
        /// The server started successfully
        /// </summary>
        Started,

        /// <summary>
        /// The server is stopping
        /// </summary>
        Stopping,

        /// <summary>
        /// The server has stopped successfully
        /// </summary>
        Stopped
    }
}
