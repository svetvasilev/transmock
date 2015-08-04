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

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// Helper class for keeping the link between a message and the connection it was received on
    /// </summary>
    internal class MessageConnectionPair
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageConnectionPair"/> class
        /// </summary>
        /// <param name="message">The message associated with the pair</param>
        /// <param name="connectionId">The Id of the connection the message was received over</param>
        public MessageConnectionPair(Message message, int connectionId)
        {
            Message = message;
            this.ConnectionId = connectionId;
        }

        /// <summary>
        /// Gets the message instance
        /// </summary>
        public Message Message { get; private set; }

        /// <summary>
        /// Gets the connection Id
        /// </summary>
        public int ConnectionId { get; private set; }
    }
}
