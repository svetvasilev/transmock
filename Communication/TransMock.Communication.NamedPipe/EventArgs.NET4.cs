﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// Event arguments for the AsyncReadEvent
    /// </summary>
    public class AsyncReadEventArgs : EventArgs
    {
        public Stream MessageStream { get; set; }

        public int ConnectionId { get; set; }
    }

    /// <summary>
    /// Event arguments for the ClientConnectedEvent
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        public int ConnectionId { get; set; }
    }
}
