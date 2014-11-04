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
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;

using BizUnit;
using BizUnit.Xaml;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Implements the logic for receiving a message from a one way enpoint
    /// which is utilizing the mock adapter.
    /// </summary>
    public class MockReceiveStep : MockStepBase
    {
        protected byte[] inBuffer;
        protected MemoryStream inStream;
        protected object syncRoot = new object();

        protected NamedPipeServerStream pipeServer;

        protected IAsyncResult asyncReadResult;
        protected IAsyncResult asyncConnectResult;

        protected string requestContent;

        public MockReceiveStep()
        {            
            SubSteps = new Collection<SubStepBase>();
            Timeout = 30;
        }
        
        public override void Execute(Context context)
        {
            lock (syncRoot)
            {
                if (!Monitor.Wait(syncRoot, 1000 * Timeout))
                    throw new TimeoutException("The step execution exceeded the alotted time");                
            }

            //If we are passed this point, then everything was processed fine
            context.LogData("MockReceiveStep received a message with content", inStream, true);
            System.Diagnostics.Debug.WriteLine(string.Format("MockReceiveStep received a message with content {0}", requestContent));
            //Here we invoke the sub steps
            foreach (var step in this.SubSteps)
            {
                step.Execute(inStream, context);
            }
            //The named pipe connection is closed
            ClosePipeServer();
            //Cleaning the inStream
            inStream.Close();
            
        }

        public override void Validate(Context context)
        {
            base.Validate(context);

            CreatePipeServer();
        }

        #region Pipe operation methdos
        /// <summary>
        /// Creates a pipe server instance
        /// </summary>
        protected void CreatePipeServer()
        {
            try
            {
                //Setting up pipe security
                PipeSecurity ps = new PipeSecurity();

                ps.AddAccessRule(new PipeAccessRule("Users",
                    PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite,
                    System.Security.AccessControl.AccessControlType.Allow));
                //Creating the named pipe server
                pipeServer = new NamedPipeServerStream(
                    _endpointUri.AbsolutePath,
                    PipeDirection.InOut, 1, 
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous, 
                    4096, 4096, ps);

                System.Diagnostics.Debug.WriteLine("Starting listening for client connections");

                //Starting the waiting for client connetions.                
                asyncConnectResult = pipeServer.BeginWaitForConnection(cb => PipeClientConnected(cb),
                    pipeServer);

            }
            finally
            {

            }

        }
        /// <summary>
        /// Invoked asyncroubously when a new client connects to the pipe server
        /// </summary>
        /// <param name="ar">The async result of the operation that triggered the method</param>
        protected void PipeClientConnected(IAsyncResult ar)
        {
            System.Diagnostics.Debug.WriteLine("Pipe client connected");

            var pipeConnection = (NamedPipeServerStream)ar.AsyncState;
            
            try
            {
                //We first end the waiting for connection
                pipeConnection.EndWaitForConnection(asyncConnectResult);
            }
            catch (System.ObjectDisposedException)
            {
                System.Diagnostics.Debug.WriteLine("Pipe has been disposed!Exiting without further processing", "TransMock.Integration.BizUnit.MockReceiveStep");
                return;
            }

            inBuffer = new byte[pipeConnection.InBufferSize];
            inStream = new MemoryStream(pipeConnection.InBufferSize);

            System.Diagnostics.Debug.WriteLine("Starting async reading from the pipe server", "TransMock.Integration.BizUnit.MockReceiveStep");
            //Starting async read by passing the named pipe conenction as a async state parameter again.
            asyncReadResult = pipeConnection.BeginRead(inBuffer, 0, inBuffer.Length, 
                cb => PipeReadAsync(cb), pipeConnection);

            System.Diagnostics.Debug.WriteLine("Async reading started", "TransMock.Integration.BizUnit.MockReceiveStep");
        }
        /// <summary>
        /// Asyncrounous callback for a read operation from the pipe
        /// </summary>
        /// <param name="ar">The async result instanced passed to the method</param>
        protected void PipeReadAsync(IAsyncResult ar)
        {
            System.Diagnostics.Debug.WriteLine("Beginning reading from the pipe", "TransMock.Integration.BizUnit.MockReceiveStep");
            //Extracting the pipe connection from which the data is being read
            var pipeConnection = (NamedPipeServerStream)ar.AsyncState;

            int bytesRead = pipeConnection.EndRead(asyncReadResult);

            lock (inStream)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Read {0} bytes from the stream", bytesRead));
                inStream.Write(inBuffer, 0, bytesRead);                
            }            

            if (!pipeConnection.IsMessageComplete)
                pipeConnection.BeginRead(inBuffer, 0, inBuffer.Length, 
                    cb => PipeReadAsync(cb), pipeConnection);
            else
            {
                System.Diagnostics.Debug.WriteLine("Message was read from the pipe. Notifying the BizUnit thread", "TransMock.Integration.BizUnit.MockReceiveStep");
                System.Diagnostics.Debug.WriteLine(string.Format("Length of inStream is {0} bytes", inStream.Length));
                //Here we need to make the message available to the step
                lock (syncRoot)
                {
                    //Rewiding the stream
                    inStream.Seek(0, SeekOrigin.Begin);
                    requestContent = _encoding.GetString(inStream.ToArray());

                    Monitor.Pulse(syncRoot);
                }

                System.Diagnostics.Debug.WriteLine("Finished message reading", "TransMock.Integration.BizUnit.MockReceiveStep");
            }
            
        }

        protected virtual void ClosePipeServer()
        {
            pipeServer.Disconnect();
            pipeServer.Close();
        }

        #endregion
    }
}
