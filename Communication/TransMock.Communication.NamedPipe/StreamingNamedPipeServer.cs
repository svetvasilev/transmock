using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// This class implements the logic of a streaming named pipe server
    /// </summary>
    public class StreamingNamedPipeServer : IStreamingServer
    {
        protected Dictionary<int, NamedPipeServerStream> pipeServers;
        protected byte[] inBuffer;
        protected MemoryStream msgStream;
        
        protected object pipeSyncLock;
        protected object pipeServersLock;

        public StreamingNamedPipeServer()
        {
            pipeSyncLock = new object();
        }

        public StreamingNamedPipeServer(string Url) : this()
        {
            URL = Url;
        }

        public string URL { get; set; }


        #region IStreamingServerImplementation
        public bool Start()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("Start called", 
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                lock (pipeSyncLock)
                {
                    pipeServers = new Dictionary<int, NamedPipeServerStream>(3);
                }
                
                CreatePipeServer();

                System.Diagnostics.Trace.WriteLine("Named pipe server started", 
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Start failed with exception: " + ex.Message,
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                return false;
            }
            
        }

        public bool Start(int timeoutMilliseconds)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("Stop called",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                lock (pipeSyncLock)
                {
                    System.Diagnostics.Debug.WriteLine("Clearing the pending pipe servers. Currently the number of servers is: " + pipeServers.Count);

                    foreach (var pipeServer in pipeServers.Values)
                    {
                        System.Diagnostics.Debug.WriteLine("Closing existing server");
                        //Close any existing pipeServers
                        pipeServer.Dispose();
                    }

                    pipeServers.Clear();
                    pipeServers = null;

                    System.Diagnostics.Debug.WriteLine("All pipe servers cleared");
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
        }

        public void Disconnect(int connectionId) 
        {
            try
            {
                NamedPipeServerStream pipeConnection;

                lock (pipeSyncLock)
                {
                    try
                    {
                        pipeConnection = pipeServers[connectionId];

                        if (pipeConnection.IsConnected)
                        {
                            pipeConnection.Disconnect();
                        }
                        //Closing the connection and disposing any resources it uses
                        pipeConnection.Dispose();
                    }
                    finally
                    {
                        pipeServers.Remove(connectionId);
                    }                    
                }
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        public byte[] ReadAllBytes()
        {
            throw new NotImplementedException();
        }

        public Stream ReadStream()
        {
            throw new NotImplementedException();
        }

        public void WriteAllBytes(int connectionId, byte[] data)
        {
            NamedPipeServerStream pipeConnection;

            lock (pipeSyncLock)
            {
                pipeConnection = pipeServers[connectionId];
            }

            if (data != null && data.Length > 0)
            {
                pipeConnection.Write(data, 0, data.Length);

                //Write the EOF bite
                pipeConnection.WriteByte(0x00);
            }            
        }

        public void WriteStream(int connectionId, Stream data)
        {
            try
            {
                NamedPipeServerStream pipeConnection;
                
                lock (pipeSyncLock)
                {
                    pipeConnection = pipeServers[connectionId];                    
                }

                int byteReadCount;
                byte[] outBuffer = new byte[4096];

                while ((byteReadCount = data.Read(outBuffer, 0, outBuffer.Length)) > 0)
                {
                    pipeConnection.Write(outBuffer, 0, byteReadCount);
                }
                //Write the EOF bite
                pipeConnection.WriteByte(0x00);
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

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
                
                //Creating the named pipe server
                NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                    URL,
                    PipeDirection.InOut, 5, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous, int.MaxValue, int.MaxValue);

                lock (pipeSyncLock)
                {
                    pipeServers.Add(pipeServer.GetHashCode(), pipeServer);
                }
                //Starting the waiting for client connetions.
                //Notice how the pipe server instance is passed as a async state object
                pipeServer.BeginWaitForConnection(cb => PipeClientConnected(cb),
                    pipeServer.GetHashCode());                
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

            int pipeConnectionId = (int)ar.AsyncState;

            NamedPipeServerStream pipeConnection;
            
            //Extract the pipe server connection from the dictionary
            lock (pipeSyncLock)
            {
                pipeConnection = pipeServers[pipeConnectionId];
            }

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
                    pipeConnection = pipeServers[state.ConnectionId];
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
                    
                    //Notify that the read was complete
                    OnReadCompleted(state.ConnectionId, state.InStream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PipeReadAsync threw an exception: " + ex.Message);
            }            
        }        
    }

    /// <summary>
    /// 
    /// </summary>
    public class AsyncReadEventArgs
    {
        public Stream MessageStream { get; set; }

        public int ConnectionId { get; set; }
    }

    /// <summary>
    /// 
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
}
