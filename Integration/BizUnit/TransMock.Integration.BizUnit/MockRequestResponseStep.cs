using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using BizUnit;
using BizUnit.Xaml;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Implements the logic for receiving a request and sending a syncrounous response
    /// to a 2-way endpoint utilizing the mock adapter
    /// </summary>
    public class MockRequestResponseStep : MockReceiveStep
    {
        public string ResponsePath { get; set; }

        public override void Execute(Context context)
        {
            base.Execute(context);
            //Here we supply the response
            using (FileStream fs = File.OpenRead(ResponsePath))
            {
                context.LogData(string.Format("Reading response content from path {0}", ResponsePath),
                    fs, true);

                byte[] outBuffer = new byte[4096];
                int bytesRead = 0;

                System.Diagnostics.Debug.WriteLine(string.Format("Starting to send the response from path {0}", ResponsePath),
                    "TransMock.Integration.BizUnit.MockRequestResponseStep");

                while ((bytesRead = fs.Read(outBuffer, 0, outBuffer.Length)) > 0)
	            {
                    pipeServer.Write(outBuffer, 0, bytesRead);
	            }
                //
                System.Diagnostics.Debug.WriteLine("Response sent to the mocked endpoint", "TransMock.Integration.BizUnit.MockRequestResponseStep");
                pipeServer.Flush();//Done with writing the response content, flushing the message
                //Waiting for the client to read the message
                pipeServer.WaitForPipeDrain();
                System.Diagnostics.Debug.WriteLine("Response received by the mocked endpoint", "TransMock.Integration.BizUnit.MockRequestResponseStep");
                //Closing the pipe server                
                pipeServer.Disconnect();
                pipeServer.Close();
            }

            context.LogInfo("Done sending the response");
        }

        public override void Validate(Context context)
        {
            base.Validate(context);

            if (string.IsNullOrEmpty(ResponsePath))
            {
                throw new ArgumentException("The ResponsePath is not defined!");
            }
        }

        protected override void ClosePipeServer()
        {
            //The method is overridden with no implementation in order to avoid
            //closing the pipe connection in the base class execute method.
            System.Diagnostics.Debug.WriteLine("ClosePipeServer invoked", "TransMock.Integration.BizUnit.MockRequestResponseStep");
        }
    }
}
