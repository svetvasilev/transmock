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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using BizUnit;
using BizUnit.TestSteps;
using BizUnit.Core.TestBuilder;

using TransMock.Communication.NamedPipes;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Implements the logic for receiving a request and sending a synchronous response
    /// to a 2-way endpoint utilizing the mock adapter
    /// </summary>
    public class MockRequestResponseStep : MockReceiveStep
    {
        /// <summary>
        /// Gets or sets the path to the file containing the response message
        /// </summary>
        public string ResponsePath { get; set; }

        /// <summary>
        /// Gets or sets the response selector function that will govern the choice of response content
        /// </summary>
        public Func<MockMessage, int, string> ResponseSelector { get; set; }

        /// <summary>
        /// Executes the step
        /// </summary>
        /// <param name="context">The execution context</param>
        public override void Execute(Context context)
        {
            base.Execute(context);            
        }

        /// <summary>
        /// Validates the step before execution
        /// </summary>
        /// <param name="context">The BizUnit execution context</param>
        public override void Validate(Context context)
        {
            base.Validate(context);

            if (string.IsNullOrEmpty(this.ResponsePath) && this.ResponseSelector == null)
            {
                throw new ArgumentException("The ResponsePath and/or ResponseSelector are not defined!");
            }
        }

        /// <summary>
        /// Sends a response message to the client
        /// </summary>
        /// <param name="context">The BizUnit execution context</param>
        protected override void SendResponse(Context context, MockMessage request, int batchIndex)
        {
            if(this.ResponseSelector != null)
            {
                context.LogInfo(
                    "Invoking response selector method.");

                this.ResponsePath = ResponseSelector(request, batchIndex);
            }

            // Here we supply the response
            using (FileStream fs = File.OpenRead(this.ResponsePath))
            {
                context.LogData(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                            "Reading response content from path {0}",
                            this.ResponsePath),
                    fs,
                    true);

            }

            var mockMessage = new MockMessage(this.ResponsePath, this.encoding);

            System.Diagnostics.Debug.WriteLine(
                "Sending response to the mocked endpoint",
                "TransMock.Integration.BizUnit.MockRequestResponseStep");

#if NET40 || NET45 || NET451
            this.pipeServer.WriteMessage(this.connectionId, mockMessage);
#elif NET462 || NET48
            var task = this.pipeServer.WriteMessageAsync(this.connectionId, mockMessage);
            task.Wait();
#endif

            context.LogInfo("Done sending the response");
        }
    }
}
