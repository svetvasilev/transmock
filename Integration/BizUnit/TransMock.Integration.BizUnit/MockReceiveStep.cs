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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

using BizUnit;
using BizUnit.Xaml;

using TransMock.Communication.NamedPipes;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Implements the logic for receiving a message from a one way endpoint
    /// which is utilizing the mock adapter.
    /// </summary>
    public class MockReceiveStep : MockStepBase, IDisposable
    {
        /// <summary>
        /// The named pipe server instance
        /// </summary>
        protected IAsyncStreamingServer pipeServer;

        /// <summary>
        /// The memory stream keeping the received message
        /// </summary>
        protected MemoryStream inStream;

        /// <summary>
        /// Synchronization object
        /// </summary>
        protected object syncRoot = new object();

        /// <summary>
        /// The contents of the request received from a client
        /// </summary>
        protected string requestContent;

        /// <summary>
        /// The id of the client connection 
        /// </summary>
        protected int connectionId = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockReceiveStep"/> class with default timeout of 30 seconds
        /// </summary>
        public MockReceiveStep()
        {   
            this.Timeout = 30;            
        }
        
        /// <summary>
        /// Executes the step
        /// </summary>
        /// <param name="context">The BizUnit execution context</param>
        public override void Execute(Context context)
        {
            try
            {
                this.WaitForRequest();

                // If we are passed this point, then everything was processed fine
                context.LogData(
                    "MockReceiveStep received a message with content", 
                    this.inStream, 
                    true);
                
                // Here we invoke the sub steps
                foreach (var step in this.SubSteps)
                {
                    step.Execute(this.inStream, context);
                }

                // Finally we supply response
                this.SendResponse(context);
            }
            finally
            {
                // The named pipe server is closed
                this.ClosePipeServer();

                // Cleaning the inStream
                this.inStream.Dispose();
            }
        }        

        /// <summary>
        /// Validates the step
        /// </summary>
        /// <param name="context">The BizUnit execution context</param>
        public override void Validate(Context context)
        {
            base.Validate(context);
            
            this.CreateServer();
        }

        /// <summary>
        /// Cleans up the step
        /// </summary>
        public override void Cleanup()
        {
            this.pipeServer.Stop();
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
        /// Implements object specific logic
        /// </summary>
        /// <param name="disposeAll">Indicates whether all or only managed objects will be disposed</param>
        protected virtual void Dispose(bool disposeAll)
        {
            if (this.pipeServer != null)
            {
                this.pipeServer.Dispose();
            }

            if (this.inStream != null)
            {
                this.inStream.Dispose();
            }
        }

        /// <summary>
        /// Creates the named pipe server
        /// </summary>
        protected virtual void CreateServer()
        {
            // We create and start the server
            this.pipeServer = new StreamingNamedPipeServer(
                this.endpointUri.AbsolutePath);

            this.pipeServer.ReadCompleted += this.pipeServer_ReadCompleted;

            this.pipeServer.Start();
        }

        /// <summary>
        /// Waits for a request to be sent by a client
        /// </summary>
        protected virtual void WaitForRequest()
        {
            lock (this.syncRoot)
            {
                if (!Monitor.Wait(this.syncRoot, 1000 * this.Timeout))
                {
                    throw new TimeoutException("The step execution exceeded the alotted time");
                }

                // If we are passed this point, then a request was successfully received
                System.Diagnostics.Debug.WriteLine(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                            "MockReceiveStep received a message with content {0}", 
                            this.requestContent));
            }
        }

        /// <summary>
        /// Sends a response to the client. Empty implementation as this is a one way receive step only.
        /// </summary>
        /// <param name="context">The BizUnit execution context</param>
        protected virtual void SendResponse(Context context)
        {
            // The base implementation is empty as this class covers only one way receive
        }

        /// <summary>
        /// Closes the pipe server instance
        /// </summary>
        protected virtual void ClosePipeServer()
        {
            this.pipeServer.Stop();
        }

        /// <summary>
        /// Handler method for the ReadCompleted event
        /// </summary>
        /// <param name="sender">The instance of the object firing the event</param>
        /// <param name="e">The event arguments</param>
        private void pipeServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            lock (this.syncRoot)
            {
                this.connectionId = e.ConnectionId;
                this.inStream = e.MessageStream as MemoryStream;
                                
                this.requestContent = this.encoding.GetString(this.inStream.ToArray());

                Monitor.Pulse(this.syncRoot);
            }
        }
    }
}
