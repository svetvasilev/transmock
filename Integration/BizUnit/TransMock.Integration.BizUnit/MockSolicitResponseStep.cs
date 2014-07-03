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
    /// Implements the logic for sending a request and receiving a syncrounous response
    /// from a 2-way endpoint utilizing the mock adapter
    /// </summary>
    public class MockSolicitResponseStep : MockSendStep
    {
        public override void Execute(Context context)
        {
            base.Execute(context);

            context.LogInfo("Waiting to read the response from the endpoint");

            int bytesRead = 0;
            byte[] inBuffer = new byte[4096];

            pipeClient.ReadMode = PipeTransmissionMode.Message;

            using (MemoryStream inStream = new MemoryStream(pipeClient.InBufferSize))
            {
                System.Diagnostics.Trace.WriteLine("Reading the response from the endpoint", "TransMock.Integration.BizUnit.MockSolicitResponseStep");
                context.LogInfo("Reading the response from the endpoint");

                bytesRead = pipeClient.Read(inBuffer, 0, inBuffer.Length);

                while (!pipeClient.IsMessageComplete)
                {
                    inStream.Write(inBuffer, 0, bytesRead);
                }

                System.Diagnostics.Debug.WriteLine("Response read!Closing the pipe client", "TransMock.Integration.BizUnit.MockSolicitResponseStep");
                pipeClient.Close();
                
                System.Diagnostics.Debug.WriteLine("Pipe client closed", "TransMock.Integration.BizUnit.MockSolicitResponseStep");

                context.LogData("The response received from the mocked endpoint is:", inStream, true);
                
                System.Diagnostics.Debug.WriteLine("Executing the substeps", "TransMock.Integration.BizUnit.MockSolicitResponseStep");

                foreach (var subStep in SubSteps)
                {
                    subStep.Execute(inStream, context);
                }

            }
            
        }

        public override void Validate(Context context)
        {
            //Call the base class implementation in order to validate the required properties
            base.Validate(context);
            
        }

        protected override void ClosePipeClient()
        {
            //Overriding with empty body to avoid closing the pipe client stream in the base.Execute() method
            System.Diagnostics.Debug.WriteLine("ClosePipeClient() invoked", "TransMock.Integration.BizUnit.MockSolicitResponseStep");
        }
    }
}
