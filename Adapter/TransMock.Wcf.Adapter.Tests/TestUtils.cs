using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

namespace TransMock.Wcf.Adapter.Tests
{
    internal class TestUtils
    {
        internal static MockMessage ReceiveResponse(NamedPipeClientStream pipeClient, Encoding encoding = null)
        {
            byte[] inBuffer = new byte[256];
            int bytesCountRead = 0;

            using (var msgStream = new MemoryStream(256))
            {
                // we try to read the response message from the pipe
                while ((bytesCountRead = pipeClient.Read(inBuffer, 0, inBuffer.Length)) > 0)
                {
                    msgStream.Write(inBuffer, 0, bytesCountRead);
                }

                return ConvertToMockMessage(msgStream, encoding ?? Encoding.UTF8);
            }
        }

        internal static MockMessage ConvertToMockMessage(MemoryStream msgStream, Encoding encoding = null)
        {
            MockMessage msg;

            msgStream.Seek(0, SeekOrigin.Begin);

            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            msg = (MockMessage)formatter.Deserialize(msgStream);

            if (encoding != null)
            {
                msg.Encoding = encoding;
            }

            return msg;
        }
    }
}
