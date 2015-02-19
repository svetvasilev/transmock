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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// This class implements the logic of a streaming named pipe server
    /// </summary>
    public class StreamingNamedPipeServer : IAsyncStreamingServer
    {
        protected Dictionary<int, NamedPipeServerStream> pipeServerConnections;
               
        protected object pipeSyncLock;        

        private ManualResetEventSlim serverStopEvent;
        private int activePipeConnectionCount = 0;      

        private ServerState serverState = ServerState.Undefined;

        /// <summary>
        /// 
        /// </summary>
        public StreamingNamedPipeServer()
        {
            pipeSyncLock = new object();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Url"></param>
        public StreamingNamedPipeServer(string Url) : this()
        {
            URL = Url;
        }

        /// <summary>
        /// 
        /// </summary>
        public string URL 
        { 
            get; set; 
        }

        #region IStreamingServerImplementation
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("Start called", 
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                serverState = ServerState.Starting;

                lock (pipeSyncLock)
                {
                    pipeServerConnections = new Dictionary<int, NamedPipeServerStream>(3);
                }
                
                CreatePipeServer();
                
                serverState = ServerState.Started;
                

                System.Diagnostics.Trace.WriteLine("Named pipe server started", 
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Start failed with exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                serverState = ServerState.Stopped;

                return false;
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("Stop called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                serverState = ServerState.Stopping;

                lock (pipeSyncLock)
                {
                    System.Diagnostics.Debug.WriteLine("Clearing the pending pipe servers. Currently the number of servers is: " + pipeServerConnections.Count,
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                    foreach (var pipeServer in pipeServerConnections.Values)
                    {
                        System.Diagnostics.Debug.WriteLine("Closing existing server",
                            "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                        //Close any existing pipeServers
                        pipeServer.Dispose();
                    }                    

                    System.Diagnostics.Debug.WriteLine("All pipe servers closed",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                }

                System.Diagnostics.Trace.WriteLine("Stop succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Stop failed with exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                
                throw;
            }
            finally
            {
                WaitForPipeServersCleanup();

                System.Diagnostics.Debug.WriteLine("Clearing the server collection",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                lock (pipeSyncLock)
                {
                    pipeServerConnections.Clear();
                    pipeServerConnections = null;
                }

                serverState = ServerState.Stopped;

                System.Diagnostics.Debug.WriteLine("Server collection cleared",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionId"></param>
        public void Disconnect(int connectionId) 
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("Disconnect() called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                NamedPipeServerStream pipeConnection;

                lock (pipeSyncLock)
                {
                    try
                    {
                        pipeConnection = pipeServerConnections[connectionId];

                        if (pipeConnection.IsConnected)
                        {
                            pipeConnection.Disconnect();
                        }
                        //Closing the connection and disposing any resources it uses
                        pipeConnection.Dispose();
                    }
                    finally
                    {
                        pipeServerConnections.Remove(connectionId);
                    }                    
                }

                System.Diagnostics.Trace.WriteLine("Disconnect() succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Disconnect() threw an exception:" + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                
                throw;
            }
            
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="data"></param>
        public void WriteAllBytes(int connectionId, byte[] data)
        {
            System.Diagnostics.Trace.WriteLine("WriteAllBytes() called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

            using (MemoryStream msgStream = new MemoryStream(data))
            {
                System.Diagnostics.Debug.WriteLine("Constructed MemoryStream of the message data.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                WriteStream(connectionId, msgStream);
            }

            System.Diagnostics.Trace.WriteLine("WriteAllBytes() succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="data"></param>
        public void WriteStream(int connectionId, Stream data)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("WriteStream() called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                NamedPipeServerStream pipeConnection;
                
                lock (pipeSyncLock)
                {
                    pipeConnection = pipeServerConnections[connectionId];                    
                }

                int byteReadCount; 
                byte[] outBuffer = new byte[4096];

                System.Diagnostics.Debug.WriteLine("Writing the message to the client.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                while ((byteReadCount = data.Read(outBuffer, 0, outBuffer.Length)) > 0)
                {
                    pipeConnection.Write(outBuffer, 0, byteReadCount);
                }

                System.Diagnostics.Debug.WriteLine("Writing the EOF byte.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                //Write the EOF bite
                pipeConnection.WriteByte(0x00);

                System.Diagnostics.Debug.WriteLine("Message sent to the client.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                pipeConnection.WaitForPipeDrain();

                System.Diagnostics.Trace.WriteLine("Message read by the client. WriteStream() succeeded",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("WriteStream() trhew an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<AsyncReadEventArgs> ReadCompleted;
        #endregion

        protected void OnClientConnected(int connectionId)
        {
            if (ClientConnected != null)
            {
                ClientConnectedEventArgs args = new ClientConnectedEventArgs(){
                    ConnectionId = connectionId
                };

                ClientConnected(this, args);
            }
        }

        protected void OnReadCompleted(int connectionId, MemoryStream memoryStream)
        {
            if (ReadCompleted != null)
            {
                AsyncReadEventArgs args = new AsyncReadEventArgs()
                {
                    ConnectionId = connectionId,
                    MessageStream = memoryStream
                };
                //Fire the event
                ReadCompleted(this, args);
            }
        }

        private void CreatePipeServer()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("CreatePipeServer() called",
                       "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                //Setting up pipe security
                PipeSecurity ps = new PipeSecurity();

                ps.AddAccessRule(new PipeAccessRule("Users",
                    PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite,
                    System.Security.AccessControl.AccessControlType.Allow));
                
                //Creating the named pipe server
                NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                    URL,
                    PipeDirection.InOut, 5, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous, 4096, 4096, ps);

                lock (pipeSyncLock)
                {
                    pipeServerConnections.Add(pipeServer.GetHashCode(), pipeServer);

                    activePipeConnectionCount++;
                }
                //Starting the waiting for client connetions.
                //Notice how the pipe server instance is passed as a async state object
                pipeServer.BeginWaitForConnection(cb => PipeClientConnected(cb),
                    pipeServer.GetHashCode());

                System.Diagnostics.Trace.WriteLine("CreatePipeServer() succeeded",
                       "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("CreatePipeServer() thre an exception: " + ex.Message,
                       "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                throw;
            }
        }

        /// <summary>
        /// Invoked asyncroubously when a new client connects to the pipe server
        /// </summary>
        /// <param name="ar">The async result of the operation that triggered the method</param>
        private void PipeClientConnected(IAsyncResult ar)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("Pipe client connected",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                int pipeConnectionId = (int)ar.AsyncState;

                NamedPipeServerStream pipeConnection;

                //Extract the pipe server connection from the dictionary
                lock (pipeSyncLock)
                {
                    pipeConnection = pipeServerConnections[pipeConnectionId];

                    activePipeConnectionCount--;
                }

                try
                {
                    //We first end the waiting for connection
                    pipeConnection.EndWaitForConnection(ar);
                }
                catch (System.ObjectDisposedException)
                {
                    System.Diagnostics.Debug.WriteLine("Pipe has been disposed!Exiting without further processing",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                    return;
                }

                byte[] inBuffer = new byte[4096];

                //Starting async read by passing the named pipe conenction as a async state parameter again.
                pipeConnection.BeginRead(inBuffer, 0,
                    inBuffer.Length, cb => PipeReadAsync(cb),
                    new AsyncReadState
                    {
                        ConnectionId = pipeConnectionId,
                        InStream = new MemoryStream(4096),
                        RawData = inBuffer
                    });

                OnClientConnected(pipeConnectionId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("PipeClientConnected() threw an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }
            finally
            {
                switch (serverState)
                {   
                    case ServerState.Started:
                        CreatePipeServer();
                        break;
                    case ServerState.Stopping:
                        SignalPipeClientDisconnect();
                        break;                    
                    default:
                        break;
                }                
            }
            
        }

        /// <summary>
        /// Asyncrounous callback for a read operation from the pipe
        /// </summary>
        /// <param name="ar">The async result instanced passed to the method</param>
        private void PipeReadAsync(IAsyncResult ar)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Beginning reading from the pipe",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                //Extracting the pipe connection from which the data is being read
                var state = ar.AsyncState as AsyncReadState;
                NamedPipeServerStream pipeConnection;                
                
                lock (pipeSyncLock)
                {
                    pipeConnection = pipeServerConnections[state.ConnectionId];
                }

                int bytesRead = pipeConnection.EndRead(ar);
                bool eofReached = false;

                System.Diagnostics.Debug.WriteLine(string.Format("Read {0} bytes", bytesRead),
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                if (bytesRead > 0)
                {
                    if (bytesRead > 2)
                    {
                        //For longer strings only the last one is EOF byte
                        eofReached = (state.RawData[bytesRead - 1] == 0x0 &&
                                        state.RawData[bytesRead - 2] != 0x0 &&
                                        state.RawData[bytesRead - 3] != 0x0);
                    }
                    if (bytesRead > 1)
                    {
                        //For Unicode case last 2 bytes are EOF
                        eofReached = (state.RawData[bytesRead - 1] == 0x0 &&
                            state.RawData[bytesRead - 2] == 0x0);
                    }
                    else if (bytesRead == 1)
                    {
                        //In case we read the last byte alone
                        eofReached = state.RawData[bytesRead - 1] == 0x0;
                    }

                    state.InStream.Write(state.RawData, 0,
                        eofReached ? bytesRead - 1 : bytesRead);

                    System.Diagnostics.Debug.WriteLine(
                        string.Format("Written {0} bytes to the internal stream",
                        eofReached ? bytesRead - 1 : bytesRead));
                }
                else
                    eofReached = true;

                if (!eofReached)
                {
                    //Not EOF, continue reading
                   pipeConnection.BeginRead(state.RawData, 0,
                        state.RawData.Length, cb => PipeReadAsync(cb), state);
                }
                else
                {
                    //EOF reached, constructing the message
                    System.Diagnostics.Debug.WriteLine("Message was read from the pipe",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
                    //Rewind the stream
                    state.InStream.Seek(0, SeekOrigin.Begin);
                    //Notify that the read was complete
                    OnReadCompleted(state.ConnectionId, state.InStream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("PipeReadAsync threw an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");
            }            
        }

        private void WaitForPipeServersCleanup() 
        {           
            System.Diagnostics.Debug.WriteLine("WaitForPipeServersCleanup called with pipeServerConnectCount: " + activePipeConnectionCount,
                "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

            serverStopEvent = new ManualResetEventSlim(false);
            serverStopEvent.Wait(TimeSpan.FromSeconds(10));

            System.Diagnostics.Debug.WriteLine("All waiting connections stopped",
                "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

            activePipeConnectionCount = 0;
                        
        }

        private void SignalPipeClientDisconnect()
        {
            if (serverStopEvent == null)
            {
                return;
            }

            if (activePipeConnectionCount == 0)
            {
                //When we have no more active pipe connections we set the event
                serverStopEvent.Set();
            }
        }
    }

    /// <summary>
    /// Event arguments for the AsyncReadEvent
    /// </summary>
    public class AsyncReadEventArgs
    {
        public Stream MessageStream { get; set; }

        public int ConnectionId { get; set; }
    }

    /// <summary>
    /// Event arguments for the ClientConnectedEvent
    /// </summary>
    public class ClientConnectedEventArgs
    {
        public int ConnectionId { get; set; }
    }

    internal class AsyncReadState
    {
        public AsyncReadState()
        {

        }

        public int ConnectionId { get; set; }

        public MemoryStream InStream { get; set; }

        public byte[] RawData { get; set; }
    }

    internal enum ServerState
    {
        Undefined,
        Starting,
        Started,
        Stopping,
        Stopped
    }
}
