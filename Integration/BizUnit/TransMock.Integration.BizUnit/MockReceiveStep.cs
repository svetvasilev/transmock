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

using TransMock.Communication.NamedPipes;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Implements the logic for receiving a message from a one way enpoint
    /// which is utilizing the mock adapter.
    /// </summary>
    public class MockReceiveStep : MockStepBase, IDisposable
    {        
        protected IAsyncStreamingServer pipeServer;
        protected MemoryStream inStream;

        protected object syncRoot = new object();

        protected string requestContent;
        protected int connectionId = 0;

        public MockReceiveStep()
        {   
            Timeout = 30;            
        }
        
        public override void Execute(Context context)
        {
            try
            {
                WaitForRequest();

                //If we are passed this point, then everything was processed fine
                context.LogData("MockReceiveStep received a message with content", inStream, true);
                
                //Here we invoke the sub steps
                foreach (var step in this.SubSteps)
                {
                    step.Execute(inStream, context);
                }
                //Finally we supply response
                SendResponse(context);
            }
            finally
            {
                //The named pipe server is closed
                ClosePipeServer();
                //Cleaning the inStream
                inStream.Dispose();
            }
        }        

        public override void Validate(Context context)
        {
            base.Validate(context);
            
            CreateServer();
        }

        public override void Cleanup()
        {
            pipeServer.Stop();
        }

        protected virtual void CreateServer()
        {
            //We create and start the server
            pipeServer = new StreamingNamedPipeServer(
                _endpointUri.AbsolutePath);

            pipeServer.ReadCompleted += pipeServer_ReadCompleted;

            pipeServer.Start();
        }

        protected virtual void WaitForRequest()
        {
            lock (syncRoot)
            {
                if (!Monitor.Wait(syncRoot, 1000 * Timeout))
                    throw new TimeoutException("The step execution exceeded the alotted time");
                //If we are passed this point, then a request was successfully received
                System.Diagnostics.Debug.WriteLine(string.Format("MockReceiveStep received a message with content {0}", requestContent));
            }
        }

        protected virtual void SendResponse(Context context)
        {
            //The base implementation is empty as this class covers only one way receive
        }

        protected virtual void ClosePipeServer()
        {
            pipeServer.Stop();
        }

        private void pipeServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            lock (syncRoot)
            {
                connectionId = e.ConnectionId;
                inStream = e.MessageStream as MemoryStream;
                                
                requestContent = _encoding.GetString(inStream.ToArray());

                Monitor.Pulse(syncRoot);
            }
        }

        #region IDisposable methdos
        public void Dispose()
        {
            Dispose(true);
        }        
        #endregion

        protected virtual void Dispose(bool disposeAll)
        {
            if (pipeServer != null)
            {
                pipeServer.Dispose();
            }

            if (inStream != null)
            {
                inStream.Dispose();
            }
        }

    }
}
