
/***************************************
//   Copyright 2019 - Svetoslav Vasilev

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

/// -----------------------------------------------------------------------------------------------------------
/// Module      :  MockOperationConfig.cs
/// Description :  This class represents an operation config agains a mocked endpoint.
/// -----------------------------------------------------------------------------------------------------------
/// 
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

namespace TransMock
{
    /// <summary>
    /// Defines the requres properties for an operation against a mocked endpoint
    /// </summary>
    internal class MessageOperationHandler
    {
        private ConcurrentDictionary<int, AsyncReadEventArgs> receivedMessages;

        private ConcurrentQueue<int> connectionsQueue;

        private ManualResetEventSlim serverMessageReceivedEvent = new ManualResetEventSlim(false);

        private ManualResetEventSlim clientConnectedEvent = new ManualResetEventSlim(false);

        public MessageOperationHandler()
        {
            connectionsQueue = new ConcurrentQueue<int>();
            receivedMessages = new ConcurrentDictionary<int, AsyncReadEventArgs>();
        }     

        public int ConnectionId
        {
            get; set;           
        }

        private int TimeoutInSeconds
        {
            get; set;
        }

        private string Url
        {
            get;set;
        }

        public SendEndpoint SendEndpoint { get; set; }

        public ReceiveEndpoint ReceiveEndpoint { get; set; }

        public TwoWayReceiveEndpoint TwoWayReceiveEndpoint { get; set; }

        public TwoWaySendEndpoint TwoWaySendEndpoint { get; set; }

        private Communication.NamedPipes.IStreamingServerAsync MockMessageServer { get; set; }

        private Communication.NamedPipes.IStreamingClientAsync MockMessageClient { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public void InitMessagingServer()
        {
            Uri serverUri = null;
            
            if (SendEndpoint != null)
            {
                serverUri = new Uri(SendEndpoint.URL);                
                Url = SendEndpoint.URL;
            }
            else if (TwoWaySendEndpoint != null)
            {
                serverUri = new Uri(TwoWaySendEndpoint.URL);                
                Url = TwoWaySendEndpoint.URL;
            }
            else
                throw new InvalidOperationException("InitMessagingServer() does not have a send endpoint to relate to!");

            InitMessagingServer(serverUri);
        }

        /// <summary>
        /// 
        /// </summary>
        public void InitMessagingClient()
        {
            Uri serverUri = null;
            
            if (ReceiveEndpoint != null)
            {
                serverUri = new Uri(ReceiveEndpoint.URL);                
            }
            else if (TwoWayReceiveEndpoint != null)
            {
                serverUri = new Uri(TwoWayReceiveEndpoint.URL);                
            }
            else
                throw new InvalidOperationException("InitMessagingClient() does not have a receive endpoint to relate to!");

            MockMessageClient = new StreamingNamedPipeClientAsync(serverUri);
        }


        /// <summary>
        /// Copies the provided source end point properties to the corresponding endpoind configured in the handler
        /// </summary>
        /// <param name="sourceEndpoint"></param>
        public void CopySendEndpointConfig(SendEndpoint sourceEndpoint)
        {
            TimeoutInSeconds = sourceEndpoint.TimeoutInSeconds;
            if (SendEndpoint != null)
            {
                SendEndpoint.TimeoutInSeconds = sourceEndpoint.TimeoutInSeconds;
            }
            else if (TwoWaySendEndpoint != null)
            {   
                TwoWaySendEndpoint.TimeoutInSeconds = sourceEndpoint.TimeoutInSeconds;
            }
            else
                throw new InvalidOperationException("CopySendEndpointConfig() does not have a send endpoint to relate to!");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceEndpoint"></param>
        public void CopyRecieveEndpointConfig(ReceiveEndpoint sourceEndpoint)
        {
            TimeoutInSeconds = sourceEndpoint.TimeoutInSeconds;
            if (ReceiveEndpoint != null)
            {
                ReceiveEndpoint.TimeoutInSeconds = sourceEndpoint.TimeoutInSeconds;
            }
            else if (TwoWayReceiveEndpoint != null)
            {
                TwoWayReceiveEndpoint.TimeoutInSeconds = sourceEndpoint.TimeoutInSeconds;
            }
            else
                throw new InvalidOperationException("CopyRecieveEndpointConfig() does not have a send endpoint to relate to!");
        }

        /// <summary>
        /// Starts the mock message server for listening for new connections
        /// </summary>
        /// <returns></returns>
        public void StartServer()
        {   
            MockMessageServer.StartAsync()
                .ConfigureAwait(false);
        }

        public async Task ConnectClientAsync()
        {
            await MockMessageClient
                    .ConnectAsync(TimeoutInSeconds * 1000)
                    .ConfigureAwait(false);
        }

        /// <summary>
        /// Reads a message from the messaging client
        /// </summary>
        /// <returns></returns>
        public async Task<MockMessage> ReadClientMessageAsync()
        {
            return await MockMessageClient.ReadMessageAsync();
        }

        public async Task WriteClientMessageAsync(MockMessage mockMessage)
        {
            await MockMessageClient
                .WriteMessageAsync(mockMessage)
                .ConfigureAwait(false);
        }

        public async Task WriteServerMessageAsync(int connectionId, MockMessage mockMessage)
        {
            await MockMessageServer
                .WriteMessageAsync(connectionId, mockMessage)
                .ConfigureAwait(false);
        }
        /// <summary>
        /// Gets the next message expected received in the messageing server
        /// </summary>
        /// <returns>An instance of <see cref="AsyncReadEventArgs"/> class containing the message that was received</returns>
        public AsyncReadEventArgs GetNextMessage()
        {
            int connectionId = ConnectionId;
            System.Diagnostics.Debug.WriteLine(
                    $"MessageOperationHandler.GetNextMessage() invoked for URL: {Url}",
                    "TransMock.MessageOperationHandler");
            // We wait for the first connection.
            // All subsequent calls would not block
            if (ConnectionId == 0)
            {
                connectionId = ConnectionId = WaitForConnectedClient();

                System.Diagnostics.Debug.WriteLine(
                    $"MessageOperationHandler.WaitForConnectedClient() returned connection id: {connectionId}",
                    "TransMock.MessageOperationHandler");
            }


            AsyncReadEventArgs receivedMessage = null;
            bool messageReceived = false;
            int retryCount = 0;

            while (!messageReceived)
            {
                // Now we wait for the reception of a message
                bool waitElapsed = serverMessageReceivedEvent.Wait(TimeoutInSeconds * 1000);                

                System.Diagnostics.Debug.WriteLine(
                    $"MessageOperationHandler.GetNextMessage() exited the wait for endpoint with URL: {Url}, connection id: {connectionId}",
                    "TransMock.MessageOperationHandler");

                try
                {
                    if (!waitElapsed)
                    {
                        System.Diagnostics.Trace.WriteLine(
                            $@"MessageOperationHandler.GetNextMessage() 
                                    did not receive in time message from send endpoint with URL: {Url}",
                            "TransMock.MessageOperationHandler");

                        throw new TimeoutException("No message received for the wait time set.");
                    }
                }
                finally
                {
                    // Resetting the event
                    serverMessageReceivedEvent.Reset();
                }

                
                // Now we destructive read the message from the message queue
                messageReceived = this.receivedMessages
                .TryRemove(
                    ConnectionId,
                    out receivedMessage);

                if (!messageReceived)
                {
                    System.Diagnostics.Trace.WriteLine(
                    $"MessageOperationHandler.GetNextMessage() did not manage to fetch message from internal collection for send endpoint with URL: {Url} and connection Id: {connectionId}. Continuing to try!",
                    "TransMock.TestMessagingClient");
                }
            }

            return receivedMessage;
        }

        /// <summary>
        /// Disconnects the underlying mock messaging client
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectClient()
        {
            await MockMessageClient
                    .DisconnectAsync()
                    .ConfigureAwait(false);
        }

        public int WaitForConnectedClient()
        {
            int connectionId = 0;
            //while (!connectionsQueue.TryDequeue(out connectionId))
            //{
                // Waiting for a while
                bool waitTimeElapsed = clientConnectedEvent.Wait(TimeoutInSeconds * 1000);

                if (!waitTimeElapsed)
                {
                    throw new TimeoutException("MessageOperationHandler.WaitForConnectedClient() timed out!");
                }
            //}

            //clientConnectedEvent.Reset();

            return ConnectionId;
        }



        /// <summary>
        /// Clrears the messageing server instance.
        /// </summary>
        public void ClearServer()
        {
            if (MockMessageServer != null)
            {
                var stopTask = MockMessageServer.StopAsync();
                stopTask.Wait();
            }            
        }

        private void InitMessagingServer(Uri serverUri)
        {
            MockMessageServer = new StreamingNamedPipeServerAsync(
                   serverUri.AbsolutePath);

            MockMessageServer.ClientConnected += ClientConnected_EventHandler;
            MockMessageServer.ReadCompleted += MessageServer_ReadCompleted;

            // We start listening for new connections right away
            // MockMessageServer.StartAsync()
            //    .ConfigureAwait(false);
        }

        private void ClientConnected_EventHandler(object sender, ClientConnectedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(
                $@"MessageOperationConfig.ClientConnected_EventHandler() 
                    invoked for connection id: {args.ConnectionId}. Current connections in the queue: {connectionsQueue.Count}",
                "TransMock.TestMessagingClient");

            ConnectionId = args.ConnectionId;
            // connectionsQueue.Enqueue(args.ConnectionId);

            // We set the event only once in order to serve for the border condition when 1st client is getting connected
            clientConnectedEvent.Set();
        }

        private void MessageServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(
                $@"MessageOperationConfig.MessageServer_ReadCompleted() 
                    invoked for connection id: {e.ConnectionId}. Active connectionId: {ConnectionId}",
                "TransMock.TestMessagingClient");

            if (e.ConnectionId == ConnectionId)
            {
                while (!this.receivedMessages.TryAdd(e.ConnectionId, e))
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Message did not get added to the collection. Attempting again!",
                        "TransMock.TestMessagingClient");
                }

                serverMessageReceivedEvent.Set();
            }
        }
    }
}