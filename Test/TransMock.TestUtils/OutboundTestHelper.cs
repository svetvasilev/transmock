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
        public static readonly byte[] EndOfMessage = new byte[3] {
            0x54, // T
            0x4d, // M
            0x04 // End of transmission
        };

        public NamedPipeServerStream pipeServer;
        public System.Threading.ManualResetEvent syncEvent;
        
        private byte[] outBuffer;
        public System.IO.MemoryStream memStream;
        public int bytesReadCount;

        public string requestXml;
        public string responseXml;
        public string responsePath;
        public Encoding responseEncoding = Encoding.UTF8;

        public OutboundTestHelper(NamedPipeServerStream pipeServer)
        {
            this.pipeServer = pipeServer;
            syncEvent = new System.Threading.ManualResetEvent(false);
            outBuffer = new byte[256];
            memStream = new System.IO.MemoryStream(512);
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
                int byteCountRead = 0, totalBytesCount = 0;
                bool eofReached = false;

                while (!eofReached)
                {
                    byteCountRead = testHelper.pipeServer.Read(outBuffer, 0, outBuffer.Length);
                    totalBytesCount += byteCountRead;

                    eofReached =  IsEndOfMessage(outBuffer, byteCountRead);

                    if (eofReached)
                    {
                        byteCountRead -= EndOfMessage.Length;
                    }

                    memStream.Write(outBuffer, 0, byteCountRead);
                }               

                System.Diagnostics.Trace.WriteLine(
                    string.Format(
                        "Finished reading from the pipe. Total bytes read: {0}", 
                        totalBytesCount), 
                    "OutboundTestHelper");
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
        public void ClientConnectedSyncronous(IAsyncResult cb, Action<OutboundTestHelper> responseHandler)
        {
            OutboundTestHelper testHelper = (OutboundTestHelper)cb.AsyncState;           
            
            try
            {
                testHelper.pipeServer.EndWaitForConnection(cb);

                if (!testHelper.pipeServer.IsConnected)
                {
                    //The method was invoked in the the case of closing the pipe. 
                    //Exiting with no room for discussion
                    return;
                }

                //We read from the pipe
                int byteCountRead = 0, totalBytesCount = 0;
                bool eofReached = false;
                
                while (!eofReached)
                {
                    byteCountRead = testHelper.pipeServer.Read(outBuffer, 0, outBuffer.Length);

                    totalBytesCount += byteCountRead;

                    eofReached = IsEndOfMessage(outBuffer, byteCountRead);

                    memStream.Write(outBuffer, 0, byteCountRead);                        
                                    
                }

                // Invoking the response handler
                responseHandler(testHelper);
            }
            finally
            {
                //We signal the event
                testHelper.syncEvent.Set();
            }
        }

        private static bool IsEndOfMessage(byte[] data, int byteCount)
        {
            if (byteCount == 0)
            {
                return true;
            }

            bool eofReached = false;

            // Take the last meaningful 3 bytes
            var eot = data.Skip(byteCount - 3)
                .Take(3)
                .ToArray();

            eofReached = eot.SequenceEqual(EndOfMessage);

            return eofReached;
        }
    }
}
