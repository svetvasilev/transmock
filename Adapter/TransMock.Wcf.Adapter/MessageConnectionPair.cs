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
