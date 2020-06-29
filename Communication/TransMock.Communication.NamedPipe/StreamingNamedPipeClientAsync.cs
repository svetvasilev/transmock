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
    public class StreamingNamedPipeClientAsync : IStreamingClientAsync
    {
        /// <summary>
        /// The instance of the pipe client stream
        /// </summary>
        protected NamedPipeClientStream pipeClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingNamedPipeClientAsync"/> class
        /// </summary>
        public StreamingNamedPipeClientAsync()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingNamedPipeClientAsync"/> class with the provided host name and pipe name
        /// </summary>
        /// <param name="hostName">The host name of the server endpoint to connect to</param>
        /// <param name="pipeName">The name of the pipe to connect to</param>
        public StreamingNamedPipeClientAsync(string hostName, string pipeName)
        {
            this.HostName = hostName;
            this.PipeName = pipeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingNamedPipeClientAsync"/> class with the provided Uri
        /// </summary>
        /// <param name="uri">The URI of the server endpoint to connect to</param>
        public StreamingNamedPipeClientAsync(Uri uri) :
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
        public async Task<bool> ConnectAsync()
        {
            return await this.ConnectAsync(int.MaxValue);
        }

        /// <summary>
        /// Attempts to connect to the server named pipe endpoint within the defined time limit
        /// </summary>
        /// <param name="timeoutMilliseconds">The time in milliseconds for establishing a connection</param>
        /// <returns>True if the connection was successful, otherwise false</returns>
        public async Task<bool> ConnectAsync(int timeoutMilliseconds)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "ConnectAsync() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                this.pipeClient = new NamedPipeClientStream(
                    this.HostName,
                    this.PipeName, 
                    PipeDirection.InOut, 
                    PipeOptions.Asynchronous);

                System.Diagnostics.Trace.WriteLine(
                    string.Format("Connecting to named pipe server at: {0}/{1}", this.HostName, this.PipeName),
                       "TransMock.Communication.NamedPipe.StreamingNamedPipeServer");

                await this.pipeClient
                    .ConnectAsync(timeoutMilliseconds);

                System.Diagnostics.Debug.WriteLine(
                    "Connected to the pipe server",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ConnectAsync() threw an exception while connetcting to {this.HostName}/{this.PipeName}: {ex.Message}",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                return false;
            }
        }

        /// <summary>
        /// Disconnects the client from the server named pipe endpoint
        /// </summary>
        public async Task DisconnectAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Trace.WriteLine(
                        "DisconnectAsync() called",
                        "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                    if (this.pipeClient != null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "Disposing the pipe stream",
                            "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                        this.pipeClient.Dispose();

                        System.Diagnostics.Debug.WriteLine(
                            "The pipe stream disposed",
                            "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");
                    }

                    System.Diagnostics.Trace.WriteLine(
                        "DisconnectAsync() succeeded",
                        "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        "DisconnectAsync() threw an exception: " + ex.Message,
                        "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                    throw;
                }
            });          
        }

        #region Read methods
        /// <summary>
        /// Reads the data sent from the server named pipe endpoint as a stream
        /// </summary>
        /// <returns>The stream object containing the data read from the server</returns>
        public async Task<System.IO.Stream> ReadStreamAsync()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "ReadStreamAsync() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                byte[] inBuffer = new byte[256];
                MemoryStream msgStream = new MemoryStream(4096);

                int byteCountRead = 0;
                bool eofReached = false;

                while (!eofReached)
                {
                    byteCountRead = await this.pipeClient
                        .ReadAsync(inBuffer, 0, inBuffer.Length);

                    eofReached = NamedPipeMessageUtils.IsEndOfMessage(
                        inBuffer, 
                        byteCountRead);

                    if (eofReached && byteCountRead > 0)
                    {
                        byteCountRead -= NamedPipeMessageUtils.EndOfMessage.Length;                       
                    }
                    
                    msgStream.Write(
                        inBuffer,
                        0,
                        byteCountRead);
                    
                }

                System.Diagnostics.Trace.WriteLine(
                    "ReadStreamAsync() succeeded",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                // Rewind the message stream to the beginning
                msgStream.Seek(0, SeekOrigin.Begin);

                return msgStream;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "ReadStreamAsync() threw exception: " + ex.Message,
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                throw;
            }
        }

        public async Task<MockMessage> ReadMessageAsync()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "ReadMessageAsync() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                using (MemoryStream msgStream = await this.ReadStreamAsync() as MemoryStream)
                {
                    if (msgStream != null)
                    {
                        var formatter = new BinaryFormatter();

                        System.Diagnostics.Trace.WriteLine(
                            "Deserializing message from the pipe.",
                            "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                        var message = (MockMessage)formatter.Deserialize(msgStream);

                        System.Diagnostics.Trace.WriteLine(
                            "ReadMessageAsync() succeeded and returning data",
                            "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                        return message;
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine(
                            "ReadMessageAsync() returning null",
                            "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                        return null;
                    }
                }                                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "ReadMessageAsync() threw exception: " + ex.Message,
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                throw;
            }
        }
        #endregion

        #region Write methods
        /// <summary>
        /// Wrties a message instance to the underlying named pipe
        /// </summary>
        /// <param name="message">The instance of a message that will be written</param>
        public async Task WriteMessageAsync(MockMessage message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "WriteMessageAsync() invoked",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                using (MemoryStream msgStream = new MemoryStream(4096))
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Constructed MemoryStream where the message will be serialized.",
                        "TransMock.Communication.NamedPipe.StreamingNamedPipeClientAsync");

                    var formatter = new BinaryFormatter();

                    formatter.Serialize(msgStream, message);

                    // Writing the TransMock EndOfMessage sequence
                    msgStream.Write(
                        NamedPipeMessageUtils.EndOfMessage,
                        0,
                        NamedPipeMessageUtils.EndOfMessage.Length);

                    // Rewinding the stream to the beginning
                    msgStream.Seek(0, SeekOrigin.Begin);

                    await this.WriteStreamAsync(msgStream);
                }

                System.Diagnostics.Trace.WriteLine(
                    "WriteMessageAsync() succeeded. Message read by the server",
                    "TransMock.Communication.NamedPipes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                   "WriteMessageAsync() threw exception: " + ex.Message,
                   "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                throw;
            }
        }
        /// <summary>
        /// Writes the data from the provided stream to the server named pipe endpoint
        /// </summary>
        /// <param name="data">The stream containing all the data to be sent to the server</param>
        public async Task WriteStreamAsync(System.IO.Stream data)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "WriteStreamAsync() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                byte[] outBuffer = new byte[4096];

                System.Diagnostics.Debug.WriteLine(
                    "Writing message to the server",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                int byteCountRead = 0;

                while ((byteCountRead = data.Read(outBuffer, 0, outBuffer.Length)) > 0)
                {
                    await this.pipeClient.WriteAsync(outBuffer, 0, byteCountRead);
                }

                // Done with writing the response content, flushing the message
                this.pipeClient.Flush();
                
                System.Diagnostics.Debug.WriteLine(
                    "Message sent to the server",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                // Waiting for the client to read the message
                this.pipeClient.WaitForPipeDrain();

                System.Diagnostics.Trace.WriteLine(
                    "WriteStreamAsync() succeeded. Message read by the server",
                    "TransMock.Communication.NamedPipes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "WriteStreamAsync() threw an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

                throw;
            }
        }       
        #endregion
        #endregion

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
                "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");

            if (this.pipeClient != null)
            {
                this.pipeClient.Dispose();
            }

            System.Diagnostics.Trace.WriteLine(
                "Dispose() succeeded",
                "TransMock.Communication.NamedPipes.StreamingNamedPipeClientAsync");
        }
    }
}
