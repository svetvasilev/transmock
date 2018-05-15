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
/// Description :  This class implements the logic of a client streaming data over named pipe.
/// -----------------------------------------------------------------------------------------------------------
/// 
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// This class implements the logic of a client streaming data over named pipe
    /// </summary>
    public class StreamingNamedPipeClient : IStreamingClient
    {
        /// <summary>
        /// The instance of the pipe client stream
        /// </summary>
        protected NamedPipeClientStream pipeClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingNamedPipeClient"/> class
        /// </summary>
        public StreamingNamedPipeClient()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingNamedPipeClient"/> class with the provided host name and pipe name
        /// </summary>
        /// <param name="hostName">The host name of the server endpoint to connect to</param>
        /// <param name="pipeName">The name of the pipe to connect to</param>
        public StreamingNamedPipeClient(string hostName, string pipeName)
        {
            this.HostName = hostName;
            this.PipeName = pipeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingNamedPipeClient"/> class with the provided Uri
        /// </summary>
        /// <param name="uri">The URI of the server endpoint to connect to</param>
        public StreamingNamedPipeClient(Uri uri) :
            this(uri.Host, uri.AbsolutePath.Substring(1))
        {
        }
        
        /// <summary>
        /// Gets or sets the host name of the server endpoint
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the named pipe name
        /// </summary>
        public string PipeName { get; set; }
                
        #region Public methods
        /// <summary>
        /// Connects to the configured server named pipe endpoint
        /// </summary>
        /// <returns>True if the connection was successful, otherwise false</returns>
        public bool Connect()
        {
            return this.Connect(int.MaxValue);
        }

        /// <summary>
        /// Attempts to connect to the server named pipe endpoint within the defined time limit
        /// </summary>
        /// <param name="timeoutMilliseconds">The time in milliseconds for establishing a connection</param>
        /// <returns>True if the connection was successful, otherwise false</returns>
        public bool Connect(int timeoutMilliseconds)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "Connect() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                this.pipeClient = new NamedPipeClientStream(
                    this.HostName,
                    this.PipeName, 
                    PipeDirection.InOut, 
                    PipeOptions.Asynchronous);

                System.Diagnostics.Trace.WriteLine(
                    string.Format("Connecting to named pipe server at: {0}/{1}", this.HostName, this.PipeName),
                       "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                this.pipeClient.Connect(timeoutMilliseconds);

                System.Diagnostics.Debug.WriteLine(
                    "Connected to the pipe server",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "Connect() threw an exception: " +
                    ex.Message,
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                return false;
            }
        }

        /// <summary>
        /// Disconnects the client from the server named pipe endpoint
        /// </summary>
        public void Disconnect()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "Disconnect() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                if (this.pipeClient != null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Disposing the pipe stream",
                        "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                    this.pipeClient.Dispose();

                    System.Diagnostics.Debug.WriteLine(
                        "The pipe stream disposed",
                        "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
                }

                System.Diagnostics.Trace.WriteLine(
                    "Disconnect() succeeded",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "Disconnect() threw an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
   
                throw;
            }            
        }

        /// <summary>
        /// Reads all the bytes sent from the server named pipe end point
        /// </summary>
        /// <returns>An array of bytes that were read from the server</returns>
        public byte[] ReadAllBytes()
        {
            System.Diagnostics.Trace.WriteLine(
                "ReadAllBytes() called",
                "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

            MemoryStream msgStream = this.ReadStream() as MemoryStream;

            if (msgStream != null)
            {
                System.Diagnostics.Trace.WriteLine(
                    "ReadAllBytes() succeeded and returning data",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                return msgStream.ToArray();
            }
            else
            {
                System.Diagnostics.Trace.WriteLine(
                    "ReadAllBytes() returning null",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                return null;
            }   
        }

        /// <summary>
        /// Reads the data sent from the server named pipe endpoint as a stream
        /// </summary>
        /// <returns>The stream object containing the data read from the server</returns>
        public System.IO.Stream ReadStream()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "ReadStream() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                byte[] inBuffer = new byte[4096];
                MemoryStream msgStream = new MemoryStream(4096);

                int byteCountRead = 0;
                bool eofReached = false;

                while (!eofReached)
                {
                    byteCountRead = this.pipeClient.Read(inBuffer, 0, inBuffer.Length);
                    
                    eofReached = NamedPipeMessageUtils.IsEndOfMessage(inBuffer, byteCountRead);

                    msgStream.Write(
                        inBuffer, 
                        0,
                        eofReached && byteCountRead > 0 ? byteCountRead - 1 : byteCountRead);                    
                }

                System.Diagnostics.Trace.WriteLine(
                    "ReadStream() succeeded",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
                
                // Rewind the message stream to the beginning
                msgStream.Seek(0, SeekOrigin.Begin);

                return msgStream;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "ReadStream() threw exception: " + ex.Message,
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                throw;
            }
        }

        public MockMessage ReadMessage()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "ReadMessage() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                var formatter = new BinaryFormatter();

                System.Diagnostics.Trace.WriteLine(
                    "Deserializing message from the pipe.",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                var message = (MockMessage) formatter.Deserialize(this.pipeClient);

                System.Diagnostics.Trace.WriteLine(
                    "ReadStream() succeeded",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                return message;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "ReadMessage() threw exception: " + ex.Message,
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                throw;
            }
        }

        /// <summary>
        /// Writes all the bytes provided to the stream and sends them to the server named pipe endpoint
        /// </summary>
        /// <param name="data">Array of bytes containing all the data that is to be sent to the server</param>
        public void WriteAllBytes(byte[] data)
        {
            System.Diagnostics.Trace.WriteLine(
                "WriteAllBytes() called",
                "TransMock.Communication.NamedPipe.StreamingNamedPipeClient");

            using (MemoryStream msgStream = new MemoryStream(data))
            {
                System.Diagnostics.Debug.WriteLine(
                    "Constructed MemoryStream of the message data.",
                    "TransMock.Communication.NamedPipe.StreamingNamedPipeClient");

                this.WriteStream(msgStream);
            }

            System.Diagnostics.Trace.WriteLine(
                "WriteAllBytes() succeeded",
                "TransMock.Communication.NamedPipe.StreamingNamedPipeClient");
        }

        /// <summary>
        /// Writes the data from the provided stream to the server named pipe endpoint
        /// </summary>
        /// <param name="data">The stream containing all the data to be sent to the server</param>
        public void WriteStream(System.IO.Stream data)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "WriteStream() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                byte[] outBuffer = new byte[4096];

                System.Diagnostics.Debug.WriteLine(
                    "Writing message to the server",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                int byteCountRead = 0;
                
                while ((byteCountRead = data.Read(outBuffer, 0, outBuffer.Length)) > 0)
                {
                    this.pipeClient.Write(outBuffer, 0, byteCountRead);
                }
                
                // Done with writing the response content, flushing the message
                this.pipeClient.Flush();

                System.Diagnostics.Debug.WriteLine(
                    "Writing the EOF byte",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
                
                // Writing the EOF byte
                this.pipeClient.WriteByte(0x00);

                System.Diagnostics.Debug.WriteLine(
                    "Message sent to the server",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
                
                // Waiting for the client to read the message
                this.pipeClient.WaitForPipeDrain();

                System.Diagnostics.Trace.WriteLine(
                    "WriteStream() succeeded. Message read by the server",
                    "TransMock.Communication.NamedPipes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "WriteStream() threw an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
                
                throw;
            }            
        }
        #endregion

        public void WriteMessage(MockMessage message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "WriteMessage() invoked",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
                var formatter = new BinaryFormatter();

                formatter.Serialize(this.pipeClient, message);

                this.pipeClient.Flush();

                System.Diagnostics.Debug.WriteLine(
                    "Message sent to the server",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                // Waiting for the client to read the message
                this.pipeClient.WaitForPipeDrain();

                System.Diagnostics.Trace.WriteLine(
                    "WriteMessage() succeeded. Message read by the server",
                    "TransMock.Communication.NamedPipes");
            }
            catch (Exception)
            {

                throw;
            }
        }
        
        /// <summary>
        /// Disposing the client
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Implements the dispose logic for the class
        /// </summary>
        /// <param name="disposeAll">Indicates whether all or only managed objects to be disposed</param>
        protected virtual void Dispose(bool disposeAll)
        {
            System.Diagnostics.Trace.WriteLine(
                "Dispose() called",
                "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

            if (this.pipeClient != null)
            {
                this.pipeClient.Dispose();
            }

            System.Diagnostics.Trace.WriteLine(
                "Dispose() succeeded",
                "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
        }
    }
}
