using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// Defines the operations of a streaming server
    /// </summary>
    public interface IStreamingServer
    {
        bool Start();

        bool Start(int timeoutMilliseconds);

        void Stop();

        void Disconnect(int connectionId);

        byte[] ReadAllBytes();

        Stream ReadStream();

        void WriteAllBytes(int connectionId, byte[] data);

        void WriteStream(int connectionId, Stream data);

        event EventHandler<ClientConnectedEventArgs> ClientConnected;

        event EventHandler<AsyncReadEventArgs> ReadCompleted;
    }    
}
