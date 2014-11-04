using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// 
    /// </summary>
    public class TransMockExecutionBeacon
    {
        private NamedPipeServerStream beaconServer;
        private IAsyncResult connectResult;
        private static bool isStarted = false;

        public TransMockExecutionBeacon()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public void StartBecon()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.StartBeacon() called.");

                if (!isStarted)
                {
                    CreatePipeServer();

                    isStarted = true;
                }

                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.StartBeacon() succeeded.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.StartBeacon() threw exception:" + ex.Message);

                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopBeacon()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.StopBeacon() called.");

                if (beaconServer != null)
                {
                    //beaconServer.EndWaitForConnection(connectResult);
                    beaconServer.Close();

                    beaconServer = null;

                    isStarted = false;
                }

                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.StopBeacon() succeeded.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.StopBeacon() threw exception:" + ex.Message);

                throw;
            }
            
        }

        private void CreatePipeServer()
        {
            System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.CreatePipeServer() called.");
            
            PipeSecurity ps = new PipeSecurity();
            
            ps.AddAccessRule(new PipeAccessRule("Users",
                PipeAccessRights.CreateNewInstance | PipeAccessRights.ReadWrite, 
                System.Security.AccessControl.AccessControlType.Allow));

            beaconServer = new NamedPipeServerStream("TransMockBeacon",
                PipeDirection.InOut,
                10,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous,
                8,8, ps);

            System.Diagnostics.Debug.WriteLine("Starting waiting for connection.");

            connectResult = beaconServer.BeginWaitForConnection(cb => ClientConnected(cb), 
                beaconServer);
        }

        private void ClientConnected(IAsyncResult cb)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.ClientConnected() called.");

                NamedPipeServerStream beaconServer = cb.AsyncState as NamedPipeServerStream;

                if (beaconServer != null)
                {
                    beaconServer.EndWaitForConnection(cb);

                    beaconServer.Disconnect();

                    System.Diagnostics.Debug.WriteLine("Disconnecting beacon client.");

                    CreatePipeServer();
                }
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.ClientConnected() threw an exception: " +
                    ex.Message);
            }
            
        }
    }
}
