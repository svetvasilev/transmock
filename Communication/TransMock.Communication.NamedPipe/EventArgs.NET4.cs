using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TransMock.Communication.NamedPipes
{
#if NET40
    /// <summary>
    /// Event arguments for the AsyncReadEvent
    /// </summary>
    public class AsyncReadEventArgs : EventArgs
    {
        public Stream MessageStream { get; set; }

        public MockMessage Message { get; set; }

        public int ConnectionId { get; set; }
    }

    /// <summary>
    /// Event arguments for the ClientConnectedEvent
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        public int ConnectionId { get; set; }
    }
#endif
}
