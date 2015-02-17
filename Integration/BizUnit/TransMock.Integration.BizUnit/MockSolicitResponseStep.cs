﻿/***************************************
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
        private Stream responseDataStream;

        public override void Execute(Context context)
        {
            try
            {
                base.Execute(context);

                System.Diagnostics.Debug.WriteLine("Executing the substeps",
                        "TransMock.Integration.BizUnit.MockSolicitResponseStep");

                foreach (var subStep in SubSteps)
                {
                    subStep.Execute(responseDataStream,
                        context);
                }
            }
            finally
            {
                if (responseDataStream != null)
                {
                    responseDataStream.Dispose();
                }
            }
            
        }

        public override void Validate(Context context)
        {
            //Call the base class implementation in order to validate the required properties
            base.Validate(context);
            
        }

        protected override void ReceiveResponse(Context context)
        {
            context.LogInfo("Waiting to read the response from the endpoint");

            System.Diagnostics.Trace.WriteLine("Reading the response from the endpoint",
                "TransMock.Integration.BizUnit.MockSolicitResponseStep");

            context.LogInfo("Reading the response from the endpoint");

            responseDataStream = pipeClient.ReadStream();

            System.Diagnostics.Debug.WriteLine("Response read!",
                    "TransMock.Integration.BizUnit.MockSolicitResponseStep");

            context.LogData("The response received from the mocked endpoint is:", responseDataStream, true);
        }
        
    }
}
