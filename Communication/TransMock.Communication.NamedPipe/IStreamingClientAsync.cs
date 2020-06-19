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
/// Module      :  IStreamingClient.cs
/// Description :  This interface defines the operations of a streaming client.
/// -----------------------------------------------------------------------------------------------------------
/// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// Defines the operations of an asynchronous streaming client
    /// </summary>
    public interface IStreamingClientAsync : IDisposable
    {
        /// <summary>
        /// Connects the client to a server endpoint
        /// </summary>
        /// <returns>True if the connection was successfully established. Otherwise false</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Connects the client to a server endpoint within a defined time period
        /// </summary>
        /// <param name="timeoutMilliseconds">The time period in milliseconds allotted to open the connection</param>
        /// <returns>True if the connection was successfully established. Otherwise false</returns>
        Task<bool> ConnectAsync(int timeoutMilliseconds);

        /// <summary>
        /// Disconnects the client from a server endpoint
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Reads the data sent from the server named pipe endpoint as a stream
        /// </summary>
        /// <returns>The stream object containing the data read from the server</returns>
        Task<Stream> ReadStreamAsync();

        /// <summary>
        /// Reads a mocke message instance from the underlying communication line
        /// </summary>
        /// <returns>An instance of <see cref="MockMessage"/> class</returns>
        Task<MockMessage> ReadMessageAsync();

        /// <summary>
        /// Writes the data from the provided stream to the server named pipe endpoint
        /// </summary>
        /// <param name="data">The stream containing all the data to be sent to the server</param>
        Task WriteStreamAsync(Stream data);

        /// <summary>
        /// Writes a mock message to the specified server named pipe endpoint
        /// </summary>
        /// <param name="message">The message instance that will be written to the connection</param>
        Task WriteMessageAsync(MockMessage message);
    }
}
