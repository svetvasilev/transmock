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
using System.IO;
using System.IO.Pipes;

using BizUnit;
using BizUnit.Xaml;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Implements the logic for sending a message to a one way endpoing utilizing the mock adapter
    /// </summary>
    public class MockSendStep : MockStepBase
    {
        /// <summary>
        /// The path to the file containing the request to be sent
        /// </summary>
        public string RequestPath { get; set; }
        /// <summary>
        /// The named pipe client stream used to communicate with the mocked endpoint
        /// </summary>
        protected NamedPipeClientStream pipeClient;

        public override void Execute(Context context)
        {
            System.Diagnostics.Debug.WriteLine("Creating a pipe client instance", "TransMock.Integration.BizUnit.MockSendStep");
            
            pipeClient = new NamedPipeClientStream(
                _endpointUri.Host, _endpointUri.AbsolutePath,
                PipeDirection.InOut, PipeOptions.Asynchronous);

            System.Diagnostics.Debug.WriteLine("Connecting to the pipe server", "TransMock.Integration.BizUnit.MockSendStep");

            pipeClient.Connect(1000 * Timeout);

            System.Diagnostics.Debug.WriteLine("Connected to the pipe server", "TransMock.Integration.BizUnit.MockSendStep");

            using (FileStream fs = File.OpenRead(RequestPath))
            {
                context.LogData(string.Format("Reading request content from path {0}", RequestPath),
                    fs, true);

                byte[] outBuffer = new byte[4096];  
                int byteCount = 0;

                System.Diagnostics.Debug.WriteLine("Starting to read the request from path {0}", RequestPath);

                while ((byteCount = fs.ReadWithoutBOM(outBuffer, 0, outBuffer.Length)) > 0)
                {
                    pipeClient.Write(outBuffer, 0, byteCount);
                }
                //
                pipeClient.Flush();//Done with writing the response content, flushing the message
                //Waiting for the client to read the message
                pipeClient.WaitForPipeDrain();
                System.Diagnostics.Debug.WriteLine("Request sent to the pipe server", "TransMock.Integration.BizUnit.MockSendStep");         
            }

            ClosePipeClient(); 
        }

        public override void Validate(Context context)
        {
            base.Validate(context);

            if (string.IsNullOrEmpty(RequestPath))
            {
                throw new ArgumentException("The RequestPath is not specified!");
            }
        }

        protected virtual void ClosePipeClient()
        {
            System.Diagnostics.Debug.WriteLine("Closing the pipe client", "TransMock.Integration.BizUnit.MockSendStep");    
            //Closing the pipe server                            
            pipeClient.Close();
            System.Diagnostics.Trace.WriteLine("PipeClient closed", "TransMock.Integration.BizUnit.MockSendStep");    
        }

        
    }
}
