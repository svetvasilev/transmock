using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO.Pipes;

namespace TransMock.TestUtils
{
    public class OutboundTestHelper
    {
        public NamedPipeServerStream pipeServer;
        public System.Threading.ManualResetEvent syncEvent;
        public byte[] outBuffer;
        public int bytesReadCount;
        public string responseXml;

        public OutboundTestHelper(NamedPipeServerStream pipeServer)
        {
            this.pipeServer = pipeServer;
            syncEvent = new System.Threading.ManualResetEvent(false);
            outBuffer = new byte[512];
        }

        public OutboundTestHelper(NamedPipeServerStream pipeServer, string responseXml)
            : this(pipeServer)
        {
            this.responseXml = responseXml;
        }

        public void ClientConnected(IAsyncResult cb)
        {
            System.Diagnostics.Trace.WriteLine("Client connected to the pipe server.", "OutboundTestHelper");

            OutboundTestHelper testHelper = (OutboundTestHelper)cb.AsyncState;

            try
            {
                testHelper.pipeServer.EndWaitForConnection(cb);

                System.Diagnostics.Trace.WriteLine("Starting to read from the pipe.", "OutboundTestHelper");
                //We read from the pipe
                testHelper.bytesReadCount = testHelper.pipeServer.Read(testHelper.outBuffer, 0, testHelper.outBuffer.Length);

                System.Diagnostics.Trace.WriteLine("Finished reading from the pipe.", "OutboundTestHelper");
            }
            finally
            {
                //We signal the event
                testHelper.syncEvent.Set();
            }
        }

        public void ClientConnectedSyncronous(IAsyncResult cb)
        {
            OutboundTestHelper testHelper = (OutboundTestHelper)cb.AsyncState;

            try
            {
                testHelper.pipeServer.EndWaitForConnection(cb);
                //We read from the pipe
                testHelper.bytesReadCount = testHelper.pipeServer.Read(testHelper.outBuffer, 0, testHelper.outBuffer.Length);

                //TODO: Set the proper encoding
                byte[] xmlBytes = Encoding.UTF8.GetBytes(testHelper.responseXml);
                //We write the response content back and flush it down the drain.
                testHelper.pipeServer.Write(xmlBytes, 0, xmlBytes.Length);
                testHelper.pipeServer.Flush();
                testHelper.pipeServer.WaitForPipeDrain();
            }
            finally
            {
                //We signal the event
                testHelper.syncEvent.Set();
            }
        }
    }
}
