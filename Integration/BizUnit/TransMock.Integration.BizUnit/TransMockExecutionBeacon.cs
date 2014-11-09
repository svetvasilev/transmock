using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;

namespace TransMock.Integration.BizUnit
{
    /// <summary>
    /// Implements beacon like functionality for enabling any process to be able to identify 
    /// whether a TransMock test case is currently being executed
    /// </summary>
    public class TransMockExecutionBeacon
    {
        private NamedPipeServerStream beaconServer;
        
        private IAsyncResult connectResult;
        
        private static bool isStarted = false;

        private static TransMockExecutionBeacon beaconInstance;

        /// <summary>
        /// Protected constructor in order to prevent others from being able to instantiate this class.
        /// </summary>
        protected TransMockExecutionBeacon()
        {

        }

        /// <summary>
        /// Starts the beacon
        /// </summary>
        public static void Start()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.Start() called.");

                if (!isStarted)
                {
                    beaconInstance = new TransMockExecutionBeacon();
                    beaconInstance.StartBecon();

                    isStarted = true;

                    System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.Start() started the beacon.");
                }

                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.StartBeacon() succeeded.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.Start() threw exception:" + ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Stops the beacon
        /// </summary>
        public static void Stop()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.Stop() called.");

                if (beaconInstance != null)
                {   
                    beaconInstance.StopBeacon();
                }

                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.Stop() succeeded.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.Stop() threw exception:" + ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Starts a new beacon instance
        /// </summary>
        protected void StartBecon()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.StartBeacon() called.");
                                
                CreatePipeServer();
                                
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.StartBeacon() succeeded.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TransMockExecutionBeacon.StartBeacon() threw exception:" + ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Stops an existing beacon instance
        /// </summary>
        protected void StopBeacon()
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

        /// <summary>
        /// Creates a new named pipe server instance for the beacon
        /// </summary>
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

        /// <summary>
        /// Handler for che asynchronous connected client operation
        /// </summary>
        /// <param name="cb">The AsyncResult instance passed as a result of the operation outcome</param>
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
