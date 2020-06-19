/***************************************
//   Copyright 2020 - Svetoslav Vasilev

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
/// Module      :  StreamingNamedPipeClientAsync.cs
/// Description : This class implements the logic of an async streaming named pipe server
/// 
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// This class implements the logic of an async streaming named pipe server
    /// </summary>
    public class StreamingNamedPipeServerAsync : IStreamingServerAsync
    {
        /// <summary>
        /// A dictionary containing all the active client connections
        /// </summary>
        protected ConcurrentDictionary<int, NamedPipeServerStream> pipeServerConnections;
        
        /// <summary>
        /// A manual reset event used for thread synchronization
        /// </summary>
        private ManualResetEventSlim serverStopEvent;

        /// <summary>
        /// The task where the server runs in
        /// </summary>
        private Task serverRunner;

        /// <summary>
        /// The current number of active pipe connections
        /// </summary>
        private int activePipeConnectionCount = 0;      

        /// <summary>
        /// The current state of the server at any given moment
        /// </summary>
        private ServerState serverState = ServerState.Undefined;

        /// <summary>
        /// The connection Id for this server instance
        /// </summary>
        private int connectionId = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingNamedPipeServerAsync"/> class
        /// </summary>
        public StreamingNamedPipeServerAsync()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingNamedPipeServerAsync"/> class that can be reached on the provided URL
        /// </summary>
        /// <param name="url">The URL on which the server will be reached</param>
        public StreamingNamedPipeServerAsync(string url) : this()
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

        #region IStreamingServerAsync Implementation
        /// <summary>
        /// Gets the connection id for this server instance
        /// </summary>
        public int ConnectionId
        {
            get
            {
                return connectionId;
            }
        }

        /// <summary>
        /// Starts the server so that the endpoint is able to receive connection requests
        /// </summary>
        /// <returns>A boolean representing whether the server started successfully or not</returns>
        public async Task<bool> StartAsync()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "Start called", 
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                this.serverState = ServerState.Starting;
                
                this.pipeServerConnections = new ConcurrentDictionary<int, NamedPipeServerStream>(100,3);

                await this.CreatePipeServer()
                       .ConfigureAwait(false);

                //serverRunner = Task.Run(async () =>
                //   await this.CreatePipeServer()
                //       .ConfigureAwait(false));

                //await Task.Delay(1); // Just to have the await in place
                
                this.serverState = ServerState.Started;
                
                System.Diagnostics.Trace.WriteLine(
                    "Named pipe server started", 
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "Start failed with exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                this.serverState = ServerState.Stopped;

                return false;
            }            
        }

        /// <summary>
        /// Stops the server which makes the endpoint unavailable for new connections
        /// </summary>
        public async Task StopAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Trace.WriteLine(
                        "Stop called",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
                    
                    this.serverState = ServerState.Stopping;

                    System.Diagnostics.Debug.WriteLine(
                        "Clearing the pending pipe servers. Currently the number of servers is: " + this.pipeServerConnections.Count,
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                    foreach (var pipeServer in this.pipeServerConnections.Values)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "Closing existing server",
                            "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                        // Close any existing pipeServers
                        pipeServer.Dispose();
                    }

                    System.Diagnostics.Debug.WriteLine(
                        "All pipe servers closed",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
                   

                    System.Diagnostics.Trace.WriteLine(
                        "Stop succeeded",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        "Stop failed with exception: " + ex.Message,
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                    throw;
                }
                finally
                {
                    this.WaitForPipeServersCleanup();

                    System.Diagnostics.Debug.WriteLine(
                        "Clearing the server collection",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
                    
                    if (this.pipeServerConnections != null)
                    {
                        this.pipeServerConnections.Clear();
                        this.pipeServerConnections = null;
                    }
                    
                    this.serverState = ServerState.Stopped;
                    connectionId = 0;

                    System.Diagnostics.Debug.WriteLine(
                        "Server collection cleared",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
                }

            });
            
        }

        /// <summary>
        /// Disconnects a client connection with the provided connection Id
        /// </summary>
        /// <param name="connectionId">The Id of the client connection that will be disconnected</param>
        public async Task DisconnectAsync(int connectionId) 
        {
            await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Trace.WriteLine(
                        "Disconnect() called",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                    NamedPipeServerStream pipeConnection;
                    
                    try
                    {
                        this.pipeServerConnections
                            .TryGetValue(connectionId, out pipeConnection);

                        if (pipeConnection.IsConnected)
                        {
                            pipeConnection.Disconnect();
                        }

                        // Closing the connection and disposing any resources it uses
                        pipeConnection.Dispose();
                    }
                    finally
                    {
                        this.pipeServerConnections.TryRemove(connectionId, out pipeConnection);
                    }                    

                    System.Diagnostics.Trace.WriteLine(
                        "Disconnect() succeeded",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        "Disconnect() threw an exception:" + ex.Message,
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                    throw;
                }
            });
                       
        }

        #region Write methods
        /// <summary>
        /// Writes an instance of a MockMessage to a given pipe stream connection
        /// </summary>
        /// <param name="connectionId">The connection Id to which to write the message</param>
        /// <param name="message">The message instance that will be written to the pipe</param>
        public async Task WriteMessageAsync(int connectionId, MockMessage message)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "WriteMessage() called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");


                var formatter = new BinaryFormatter();

                using (MemoryStream msgStream = new MemoryStream(4096))
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Serializing the message to a stream.",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                    formatter.Serialize(msgStream, message);

                    // Writing EndOfMessage sequence
                    msgStream.Write(
                        NamedPipeMessageUtils.EndOfMessage,
                        0,
                        NamedPipeMessageUtils.EndOfMessage.Length);

                    msgStream.Seek(0, SeekOrigin.Begin);

                    await this.WriteStreamAsync(connectionId, msgStream);
                }

                System.Diagnostics.Trace.WriteLine(
                    "WriteMessage() succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "WriteStream() trhew an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                throw;
            }
        }

        /// <summary>
        /// Writes the contents of the supplied stream to a connection with the provided Id
        /// </summary>
        /// <param name="connectionId">The Id of the connection where the data shall be written to</param>
        /// <param name="data">The data to be written to the connection represented as a stream</param>
        public async Task WriteStreamAsync(int connectionId, Stream data)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "WriteStream() called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                NamedPipeServerStream pipeConnection;
                
                while(!this.pipeServerConnections
                    .TryGetValue(connectionId, out pipeConnection))
                {
                    System.Diagnostics.Trace.WriteLine(
                        "WriteStream() did not acquire pipe server. Continuing...",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
                }
                

                int byteReadCount = 0; 
                byte[] outBuffer = new byte[4096];

                System.Diagnostics.Debug.WriteLine(
                    "Writing the message to the client.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                while ((byteReadCount = data.Read(outBuffer, 0, outBuffer.Length)) > 0)
                {
                    await pipeConnection.WriteAsync(outBuffer, 0, byteReadCount);
                }

                System.Diagnostics.Debug.WriteLine(
                    "Message sent to the client.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                pipeConnection.WaitForPipeDrain();

                System.Diagnostics.Trace.WriteLine(
                    "Message read by the client. WriteStream() succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "WriteStream() trhew an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                throw;
            }
        }
        #endregion
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
                "TransMock.Communication.NamedPipes.StreamingNamedPipeServerAsync");

            this.serverStopEvent.Dispose();
            
            this.pipeServerConnections = null;

            System.Diagnostics.Trace.WriteLine(
                "Dispose() succeeded",
                "TransMock.Communication.NamedPipes.StreamingNamedPipeServerAsync");
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
                var formatter = new BinaryFormatter();
                var message = (MockMessage)formatter.Deserialize(memoryStream);

                AsyncReadEventArgs args = new AsyncReadEventArgs()
                {
                    ConnectionId = connectionId,
                    MessageStream = memoryStream,
                    Message = message
                };

                // Fire the event
                this.ReadCompleted(this, args);
            }
        }

        /// <summary>
        /// Creates an instance of the pipe server
        /// </summary>
        private async Task CreatePipeServer()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "CreatePipeServer() called",
                       "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                // Setting up pipe security
                PipeSecurity ps = new PipeSecurity();
                SecurityIdentifier si = new SecurityIdentifier(
                    WellKnownSidType.BuiltinUsersSid,
                    null);

                ps.AddAccessRule(
                    new PipeAccessRule(
                        si,
                        PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite,
                        System.Security.AccessControl.AccessControlType.Allow));

                System.Diagnostics.Trace.WriteLine(
                    string.Format("Creating named pipe server with URL: {0}", this.URL),
                       "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
                
                // Creating the named pipe server
                NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                    this.URL,
                    PipeDirection.InOut, 
                    NamedPipeServerStream.MaxAllowedServerInstances, 
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous, 
                    4096, 
                    4096, 
                    ps);

                
                connectionId = pipeServer.GetHashCode();
                this.pipeServerConnections.TryAdd(connectionId, pipeServer);

                System.Diagnostics.Debug.WriteLine(
                    $"CreatePipeServer() created a new pipe server instance with id: {connectionId}",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                this.activePipeConnectionCount++;
                // This blocks completely
                await pipeServer.WaitForConnectionAsync()
                    .ContinueWith(
                        async t =>
                        {
                            //System.Diagnostics.Trace.WriteLine(
                            //    "CreatePipeServer() - client connected",
                            //    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                            //await PipeReadAsync(
                            //    new AsyncReadState
                            //    {
                            //        ConnectionId = connectionId,
                            //        InStream = new MemoryStream(4096)
                            //    })
                            await PipeClientConnected(connectionId)
                            .ConfigureAwait(false);
                        })
                    .ConfigureAwait(false);                    
                
                
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "CreatePipeServer() threw an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                throw;
            }
        }

        /// <summary>
        /// Invoked asynchronously when a new client connects to the pipe server
        /// </summary>
        /// <param name="ar">The async result of the operation that triggered the method</param>
        private async Task PipeClientConnected(int pipeConnectionId)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "PipeClientConnected() start listening for new incoming connection.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                //int pipeConnectionId = (int)ar.AsyncState;

                //NamedPipeServerStream pipeConnection;

                //while(!this.pipeServerConnections.TryRemove(
                //    pipeConnectionId, out pipeConnection))
                //{
                //    System.Diagnostics.Trace.WriteLine(
                //        $"PipeClientConnected() not able to fetch connection for id {connectionId}. Trying again",
                //        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
                //}

                this.activePipeConnectionCount--;
                
                this.OnClientConnected(pipeConnectionId);

                // byte[] inBuffer = new byte[4096];

                await PipeReadAsync(
                    new AsyncReadState
                    {
                        ConnectionId = connectionId,
                        InStream = new MemoryStream(4096)
                    })
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "PipeClientConnected() threw an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
            }
            finally
            {
                switch (this.serverState)
                {
                    case ServerState.Started:
                        // If the server is Started a new pipe server instance is created
                        await this.CreatePipeServer();                            
                        break;
                    case ServerState.Stopping:
                        // If the server is Stoppin all the connected clients will be disconnected
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
        private async Task PipeReadAsync(AsyncReadState state)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Beginning reading from the pipe. Thread id: {Thread.CurrentThread.ManagedThreadId}.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                NamedPipeServerStream pipeConnection;                
                
                System.Diagnostics.Debug.WriteLine(
                    $@"Getting connection for client id: {state.ConnectionId}. 
                        Thread id: {Thread.CurrentThread.ManagedThreadId}.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                while(!this.pipeServerConnections
                    .TryGetValue(state.ConnectionId, out pipeConnection))
                {
                    System.Diagnostics.Debug.WriteLine(
                    $@"Did not manage to acwuire connection for client id: {state.ConnectionId}. 
                        Continuing to try...",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
                }

                if (!pipeConnection.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Pipe has not been connected yet! Exiting",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                    return;
                }
                    
                

                byte[] buffer = new byte[4096];
                int bytesRead = await pipeConnection.ReadAsync(buffer, 0, buffer.Length);
                bool eofReached = false;
                
                System.Diagnostics.Debug.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Read {0} bytes", 
                        bytesRead),
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                eofReached = NamedPipeMessageUtils.IsEndOfMessage(
                    buffer, 
                    bytesRead);

                System.Diagnostics.Debug.WriteLine(
                        $@"eofReached is: {eofReached}. 
                            Thread id: {Thread.CurrentThread.ManagedThreadId}.",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                if (eofReached && bytesRead > 0)
                {
                    bytesRead -= NamedPipeMessageUtils.EndOfMessage.Length;
                    // We take tha contents without the EndOfMessage sequence
                    buffer = buffer
                        .Take(bytesRead)
                        .ToArray();
                }
                
                state.InStream.Write(
                    buffer, 
                    0,
                    bytesRead);

                System.Diagnostics.Debug.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture, 
                        "Written {0} bytes to the internal stream",
                         bytesRead),
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
               
                if (!eofReached)
                {
                    // Not EOF, continue reading
                    await PipeReadAsync(state);
                }
                else
                {
                    // EOF reached, constructing the message
                    System.Diagnostics.Debug.WriteLine(
                        "Message was read from the pipe",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

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
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");
            }            
        }        

        /// <summary>
        /// Waits for the named pipe server stream instances to clean-up
        /// </summary>
        private void WaitForPipeServersCleanup() 
        {           
            System.Diagnostics.Debug.WriteLine(
                "WaitForPipeServersCleanup called with pipeServerConnectCount: " + this.activePipeConnectionCount,
                "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

            // Setting up thre signalling mechanism in case there are still active connections
            if (this.activePipeConnectionCount > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Setting up stop event",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

                this.serverStopEvent = new ManualResetEventSlim(false);
                this.serverStopEvent.Wait(TimeSpan.FromSeconds(10));                
            }

            System.Diagnostics.Debug.WriteLine(
                "All waiting connections stopped",
                "TransMock.Communication.NamedPipe.StreamingNamedPipeServerAsync");

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
}
