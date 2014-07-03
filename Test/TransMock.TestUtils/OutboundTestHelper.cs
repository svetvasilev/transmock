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
using System.Text;

using System.IO.Pipes;

namespace TransMock.TestUtils
{
    /// <summary>
    /// Helper class for the cases when outbound in regards to the adapter communication is tested
    /// </summary>
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

        /// <summary>
        /// The callback method for when client is connected to the pipe
        /// </summary>
        /// <param name="cb">The asynchronous result of the connect operation</param>
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

        /// <summary>
        /// The callback method for when clien is connected to the pipe in two-way communication scenarios
        /// </summary>
        /// <param name="cb">The asynchronous result of the connect operation</param>
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
