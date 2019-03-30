using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

namespace TransMock
{
    internal class MessageOperationConfig
    {
        public SendEndpoint SendEndpoint { get; set; }

        public ReceiveEndpoint ReceiveEndpoint { get; set; }

        public TwoWayReceiveEndpoint TwoWayReceiveEndpoint { get; set; }

        public TwoWaySendEndpoint TwoWaySendEndpoint { get; set; }

        public Communication.NamedPipes.StreamingNamedPipeServer MockMessageServer { get; set; }

        public Communication.NamedPipes.StreamingNamedPipeClient MockMessageClient { get; set; }

    }
}