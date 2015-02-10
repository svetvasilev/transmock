using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// Defines the operations of a streaming client
    /// </summary>
    public interface IStreamingClient
    {
        bool Connect();

        bool Connect(int timeoutMilliseconds);

        void Disconnect();

        byte[] ReadAllBytes();

        Stream ReadStream();

        void WriteAllBytes(byte[] data);

        void WriteStream(Stream data);
    }
}
