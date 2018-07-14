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
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

using BizUnit;
using BizUnit.TestSteps;

using TransMock.Communication.NamedPipes;
using BizUnit.Core.TestBuilder;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Implements the logic for sending a message to a one way endpoint utilizing the mock adapter
    /// </summary>
    public class MockSendStep : MockStepBase, IDisposable
    {
        /// <summary>
        /// The named pipe client stream used to communicate with the mocked endpoint
        /// </summary>
        protected StreamingNamedPipeClient pipeClient;

        /// <summary>
        /// Gets or sets the path to the file containing the request to be sent
        /// </summary>
        public string RequestPath { get; set; }

        public Dictionary<string, string> MessageProperties { get; set; }

        /// <summary>
        /// Initializes a new instance of MockSendStep
        /// </summary>
        public MockSendStep()
        {
            this.MessageProperties = new Dictionary<string, string>(3);
        }
        /// <summary>
        /// Executes the step's logic
        /// </summary>
        /// <param name="context">The execution context</param>
        public override void Execute(Context context)
        {
            try
            {
                this.CreatePipeClient();

                this.SendRequest(context);

                this.ReceiveResponse(context);
            }
            finally
            {
                this.ClosePipeClient(); 
            }
        }

        /// <summary>
        /// Validates the step before execution
        /// </summary>
        /// <param name="context">The execution context</param>
        public override void Validate(Context context)
        {
            base.Validate(context);

            if (string.IsNullOrEmpty(this.RequestPath))
            {
                throw new ArgumentException("The RequestPath is not specified!");
            }
        }

        #region IDisposable methdos
        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion

        /// <summary>
        /// Implements the objects disposal logic
        /// </summary>
        /// <param name="disposeAll">Indicates whether all or only managed objects will be disposed</param>
        protected virtual void Dispose(bool disposeAll)
        {
            if (this.pipeClient != null)
            {
                this.pipeClient.Dispose();
            }
        }

        /// <summary>
        /// Creates an instance of the named pipe client
        /// </summary>
        protected virtual void CreatePipeClient()
        {
            System.Diagnostics.Debug.WriteLine(
                "Creating a pipe client instance", 
                "TransMock.Integration.BizUnit.MockSendStep");

            this.pipeClient = new StreamingNamedPipeClient(
                endpointUri.Host, 
                endpointUri.AbsolutePath);

            System.Diagnostics.Debug.WriteLine(
                "Connecting to the pipe server", 
                "TransMock.Integration.BizUnit.MockSendStep");

            this.pipeClient.Connect(1000 * this.Timeout);

            System.Diagnostics.Debug.WriteLine(
                "Connected to the pipe server", 
                "TransMock.Integration.BizUnit.MockSendStep");
        }

        /// <summary>
        /// Sends a request to the server endpoint
        /// </summary>
        /// <param name="context">The execution context for the step</param>
        protected virtual void SendRequest(Context context)
        {
            using (FileStream fs = File.OpenRead(this.RequestPath))
            {
                context.LogData(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                            "Reading request content from path {0}",
                            this.RequestPath),
                    fs,
                    true);
            }

            System.Diagnostics.Debug.WriteLine(
                "Sending request to the pipe server",
                "TransMock.Integration.BizUnit.MockSendStep");

            var mockMessage = new MockMessage(this.RequestPath, this.encoding);

            mockMessage.Properties = this.MessageProperties;

            this.pipeClient.WriteMessage(mockMessage);

            System.Diagnostics.Debug.WriteLine(
                "Request sent to the pipe server", 
                "TransMock.Integration.BizUnit.MockSendStep");            
        }

        /// <summary>
        /// Receives a response from the server endpoint
        /// </summary>
        /// <param name="context">The execution context</param>
        protected virtual void ReceiveResponse(Context context)
        {
            // Empty implementation for the one way send step
        }

        /// <summary>
        /// Closes the pipe client
        /// </summary>
        protected virtual void ClosePipeClient()
        {
            System.Diagnostics.Debug.WriteLine(
                "Closing the pipe client", 
                "TransMock.Integration.BizUnit.MockSendStep");

            if (this.pipeClient != null)
            {
                // Closing the pipe server                            
                this.pipeClient.Disconnect();
            }

            System.Diagnostics.Trace.WriteLine(
                "PipeClient closed", 
                "TransMock.Integration.BizUnit.MockSendStep");    
        }        
    }
}
