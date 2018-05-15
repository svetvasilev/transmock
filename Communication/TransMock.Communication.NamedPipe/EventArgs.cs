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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// Event arguments for the AsyncReadEvent
    /// </summary>
    public class AsyncReadEventArgs
    {
        /// <summary>
        /// Gets or sets the message stream
        /// </summary>
        public Stream MessageStream { get; set; }

        /// <summary>
        /// Gets or sets the connection Id
        /// </summary>
        public int ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the message instance
        /// </summary>
        public MockMessage Message { get; set; }
    }

    /// <summary>
    /// Event arguments for the ClientConnectedEvent
    /// </summary>
    public class ClientConnectedEventArgs
    {
        /// <summary>
        /// Gets or sets the connection Id
        /// </summary>
        public int ConnectionId { get; set; }
    }
}
