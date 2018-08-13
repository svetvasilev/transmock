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
using BizUnit.TestSteps;

using TransMock.Communication.NamedPipes;
using TransMock.Integration.BizUnit.Validation;
using BizUnit.Core.TestBuilder;

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

        protected Queue<AsyncReadEventArgs> receivedMessagesQueue;

        /// <summary>
        /// Gets or sets the number of messages expected as a resulf of de-batching scenario
        /// </summary>
        public int DebatchedMessageCount
        { 
            get; set; 
        }

        /// <summary>
        /// Gets or sets the validation mode in case multiple messages are received by the same 
        /// test step instance
        /// </summary>
        public MultiMessageValidationMode ValidationMode 
        { 
            get; set; 
        }

        /// <summary>
        /// Gets or sets the dictionary with the sub steps colleciton specific to each received message.
        /// It is used only in case the ValidationMode is set to MultiMessageValidationMode.Cascading
        /// </summary>
        public Dictionary<int, Collection<SubStepBase>> CascadingSubSteps 
        { 
            get; set; 
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="MockReceiveStep"/> class with default timeout of 30 seconds
        /// </summary>
        public MockReceiveStep()
        {   
            this.Timeout = 30;
            // Setting the Debatched message count to 1 as default.             
            this.DebatchedMessageCount = 1;

            this.ValidationMode = MultiMessageValidationMode.Serial;

            this.CascadingSubSteps = new Dictionary<int, Collection<SubStepBase>>();

            this.receivedMessagesQueue = new Queue<AsyncReadEventArgs>(3);
        }
        
        /// <summary>
        /// Executes the step
        /// </summary>
        /// <param name="context">The BizUnit execution context</param>
        public override void Execute(Context context)
        {   
            try{
                // The processing happens in a synchronization block in order to avoid
                // incorrect thread syncronization with the threads from the pipe server
                lock (this.syncRoot)
                {
                    // In case of de-batch scenario we will wait for as many request messages as 
                    // the number of messages specified in DebatchedMessageCount
                    for (int i = 0; i < this.DebatchedMessageCount; i++)
                    {
                    
                        this.WaitForRequest();

                        var receivedMessage = this.receivedMessagesQueue.Dequeue();

                        // If we are passed this point, then everything was processed fine
                        context.LogData(
                            "MockReceiveStep received a message with content",
                            receivedMessage.Message.Body);

                        // Here we invoke the sub steps
                        switch (this.ValidationMode)
                        {
                            case MultiMessageValidationMode.Cascading:
                                // Performing cascading validation
                                this.CascadingValidation(
                                    receivedMessage.Message, 
                                    context, 
                                    i);
                                break;
                            case MultiMessageValidationMode.Serial:
                            default:     
                                // Performing serial validation
                                this.SerialValidation(
                                    receivedMessage.Message, 
                                    context);
                                break;
                        }

                        //Set the connection Id for the response to be sent over the correct pipe
                        this.connectionId = receivedMessage.ConnectionId;

                        // Finally we supply response
                        this.SendResponse(context, receivedMessage.Message, i);  
                    }
                }
            }
            finally
            {
                // The named pipe server is closed
                this.ClosePipeServer();

                // Cleaning the inStream
                if (this.inStream != null)
                {
                    this.inStream.Dispose();
                }                
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
            if (pipeServer != null)
            {
                this.pipeServer.Stop();
            }

            if (this.CascadingSubSteps != null)
            {
                this.CascadingSubSteps.Clear();
                this.CascadingSubSteps = null;
            }

            if (this.receivedMessagesQueue != null)
            {
                this.receivedMessagesQueue.Clear();
                this.receivedMessagesQueue = null;
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
            lock (this.syncRoot)
            {
                this.pipeServer = new StreamingNamedPipeServer(
                this.endpointUri.AbsolutePath);

                this.pipeServer.ReadCompleted += this.pipeServer_ReadCompleted;

                this.pipeServer.Start();
            }
            
        }

        /// <summary>
        /// Waits for a request to be sent by a client
        /// </summary>
        protected virtual void WaitForRequest()
        {  
            if (!Monitor.Wait(this.syncRoot, 1000 * this.Timeout))
            {
                throw new TimeoutException("The step execution exceeded the alotted time");
            }

            // If we are passed this point, then a request was successfully received
            System.Diagnostics.Debug.WriteLine(
                string.Format(
                    CultureInfo.CurrentUICulture,
                        "WaitForRequest exited the wait queue as a result of message reception."));
            
        }

        /// <summary>
        /// Sends a response to the client. Empty implementation as this is a one way receive step only.
        /// </summary>
        /// <param name="context">The BizUnit execution context</param>
        protected virtual void SendResponse(Context context, MockMessage request, int batchIndex)
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
        /// Performs serial validation of a message that has been received by the step
        /// </summary>
        /// <param name="message">The message object that will be validated</param>
        /// <param name="context">The BizUnit context isntance</param>
        private void SerialValidation(MockMessage message, Context context)
        {
            foreach (var step in this.SubSteps)
            {
                if (step is LambdaValidationStep)
                {
                    ((LambdaValidationStep)step).Execute(message, context);
                }
                else
                {
                    step.Execute(message.BodyStream, context);
                }                
            }
        }

        /// <summary>
        /// Performs cascading validation of a message that has been received by the step
        /// </summary>
        /// <param name="message">The message object that will be validated</param>
        /// <param name="context">The BizUnit context isntance</param>
        /// <param name="index">The index at which to extract the collection of validation sub steps</param>
        private void CascadingValidation(MockMessage message, Context context, int index)
        {
            if (this.CascadingSubSteps.Count > 0)
            {
                var validationSubSteps = this.CascadingSubSteps[index];

                if (validationSubSteps != null)
                {
                    foreach (var step in validationSubSteps)
                    {
                        if (step is LambdaValidationStep)
                        {
                            ((LambdaValidationStep)step).Execute(message, context);
                        }
                        else
                        {
                            step.Execute(message.BodyStream, context);
                        }
                    }
                }
            }
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
                this.receivedMessagesQueue.Enqueue(e);
                                
                // this.requestContent = this.encoding.GetString(this.inStream.ToArray());

                Monitor.Pulse(this.syncRoot);
            }
        }
    }
}
