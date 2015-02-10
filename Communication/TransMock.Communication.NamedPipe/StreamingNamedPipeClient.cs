using System;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransMock.Communication.NamedPipes
{
    /// <summary>
    /// This class implements the logic of a client streaming over named pipe
    /// </summary>
    public class StreamingNamedPipeClient : IStreamingClient, IDisposable
    {
        protected NamedPipeClientStream pipeClient;        

        public StreamingNamedPipeClient()
        {

        }

        public StreamingNamedPipeClient(string hostName, string pipeName)
        {
            HostName = hostName;
            PipeName = pipeName;
        }

        public StreamingNamedPipeClient(Uri uri) :
            this(uri.Host, uri.AbsolutePath.Substring(1))
        {
                
        }

        public string HostName { get; set; }

        public string PipeName { get; set; }
        
        #region MyRegion
        public bool Connect()
        {
            return Connect(int.MaxValue);
        }

        public bool Connect(int timeoutMilliseconds)
        {
            try
            {
                pipeClient = new NamedPipeClientStream(HostName,
                    PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                pipeClient.Connect(timeoutMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Connect() threw an exception: " +
                       ex.Message,
                       "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("Disconnect() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                if (pipeClient != null)
                {
                    System.Diagnostics.Debug.WriteLine("Disposing the pipe stream",
                        "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                    pipeClient.Dispose();

                    System.Diagnostics.Debug.WriteLine("The pipe stream disposed",
                        "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
                }

                System.Diagnostics.Trace.WriteLine("Disconnect() succeeded",
                        "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Disconnect() threw an exception: " +
                       ex.Message,
                       "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
   
                throw;
            }
            
        }

        public byte[] ReadAllBytes()
        {
            MemoryStream msgStream = ReadStream() as MemoryStream;

            if (msgStream != null)
            {
                return msgStream.ToArray();
            }
            else
                return null;
        }

        public System.IO.Stream ReadStream()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("ReadStream() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                byte[] inBuffer = new byte[4096];
                MemoryStream msgStream = new MemoryStream(4096);

                int byteCountRead = pipeClient.Read(inBuffer, 0, inBuffer.Length);
                    bool eofReached = false;

                while (!eofReached)
                {
                    byteCountRead = pipeClient.Read(inBuffer, 0, inBuffer.Length);

                    if (byteCountRead > 0)
                    {
                        if (byteCountRead > 2)
                        {
                            //For longer strings only the last one is EOF byte
                            eofReached = (inBuffer[byteCountRead - 1] == 0x0 &&
                                            inBuffer[byteCountRead - 2] != 0x0 &&
                                            inBuffer[byteCountRead - 3] != 0x0);
                        }
                        else if (byteCountRead > 1)
                        {
                            //In case of Unicode the last 2 bytes are EOF
                            eofReached = (inBuffer[byteCountRead - 1] == 0x0 &&
                                inBuffer[byteCountRead - 2] == 0x0);
                        }
                        else if (byteCountRead == 1)
                        {
                            //In case the EOF was read alone
                            eofReached = inBuffer[byteCountRead - 1] == 0x0;
                        }

                        msgStream.Write(inBuffer, 0,
                            eofReached ? byteCountRead - 1 : byteCountRead);
                    }
                    else
                    {
                        eofReached = true;
                    }
                }

                System.Diagnostics.Trace.WriteLine("ReadStream() succeeded",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                return msgStream;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("ReadStream() threw exception: " +
                    ex.Message,
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                throw;
            }
        }

        public void WriteAllBytes(byte[] data)
        {
            throw new NotImplementedException();
        }

        public void WriteStream(System.IO.Stream data)
        {
            try
            {
                System.Diagnostics.Trace.WriteLine("WriteStream() called",
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");

                byte[] outBuffer = new byte[4096];

                int byteCountRead = 0;
                //while ((byteCount = fs.ReadWithoutBOM(outBuffer, 0, outBuffer.Length)) > 0)
                while ((byteCountRead = data.Read(outBuffer, 0, outBuffer.Length)) > 0)
                {
                    pipeClient.Write(outBuffer, 0, byteCountRead);
                }
                //Done with writing the response content, flushing the message
                pipeClient.Flush();
                //Writing the EOF byte
                pipeClient.WriteByte(0x00);
                //Waiting for the client to read the message
                pipeClient.WaitForPipeDrain();

                System.Diagnostics.Trace.WriteLine("WriteStream() succeeded",
                    "TransMock.Communication.NamedPipes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("WriteStream() threw an exception: " + ex.Message,
                    "TransMock.Communication.NamedPipes.StreamingNamedPipeClient");
                
                throw;
            }
            
        }
        #endregion
        
        public void Dispose()
        {
            if (pipeClient != null)
            {
                pipeClient.Dispose();
            }
        }
    }
}
