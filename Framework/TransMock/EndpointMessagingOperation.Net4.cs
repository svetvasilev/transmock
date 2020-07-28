
/***************************************
//   Copyright 2020 - Svetoslav Vasilev

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
/// Module      :  EndpointMessagingHandler.Net4.cs
/// Description :  This class handles messaging operations for a particular mocked endpoint. (for .Net 4.0)
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
    /// Implements the logic for handling messaging operations against a mocked endpoint
    /// </summary>
    internal class EndpointMessagingOperation
    {
        /// <summary>
        /// Collection containing the received messages through the <see cref="IStreamingServerAsync"/> instance
        /// </summary>
        private ConcurrentDictionary<int, AsyncReadEventArgs> receivedMessages;

        /// <summary>
        /// Synchronization construct
        /// </summary>      
        private SemaphoreSlim receiveMessageSemaphore = new SemaphoreSlim(0, 1000);

        /// <summary>
        /// Creates a new instance of <see cref="EndpointMessagingOperation"/> class
        /// </summary>
        public EndpointMessagingOperation()
        {
            receivedMessages = new ConcurrentDictionary<int, AsyncReadEventArgs>();
        }

        public EndpointMessagingOperation(EndpointMessagingOperation source)
        {
            Url = source.Url;
            ReceiveEndpoint = source.ReceiveEndpoint;
            TwoWayReceiveEndpoint = source.TwoWayReceiveEndpoint;
            SendEndpoint = source.SendEndpoint;
            TwoWaySendEndpoint = source.TwoWaySendEndpoint;
        }     

        /// <summary>
        /// The current connection id for message reception operation
        /// </summary>
        public int ConnectionId
        {
            get; set;           
        }

        /// <summary>
        /// The timeout in seconds for the configured mock endpoint
        /// </summary>
        private int TimeoutInSeconds
        {
            get; set;
        }

        /// <summary>
        /// The URL adress forthe configured mock endpoint
        /// </summary>
        private string Url
        {
            get;set;
        }

        /// <summary>
        /// Gets or sets the instance of the <see cref="SendEndpoint"/> endpoint that performs a 1-way send operation
        /// </summary>
        public SendEndpoint SendEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the instance of <see cref="ReceiveEndpoint"/> endpoint that performs a 1-way receive operation
        /// </summary>
        public ReceiveEndpoint ReceiveEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the instance of <see cref="TwoWayReceiveEndpoint"/> endpoint that performs a 2-way receive operation
        /// </summary>
        public TwoWayReceiveEndpoint TwoWayReceiveEndpoint { get; set; }

        /// <summary>
        /// Gets or sets theinstance of <see cref="TwoWaySendEndpoint" /> endpoint that performs a 2-way send operation
        /// </summary>
        public TwoWaySendEndpoint TwoWaySendEndpoint { get; set; }

        /// <summary>
        /// Sets or gets an instance of the <see cref="IStreamingServerAsync"/> implementation for communicating with sending endpoints of tested services
        /// </summary>
        private Communication.NamedPipes.IAsyncStreamingServer MockMessageServer { get; set; }

        /// <summary>
        /// Gets or sets an insta of the <see cref="IStreamingClientAsync"/> implementation for communicating with receiving endpoints of tested services
        /// </summary>
        private Communication.NamedPipes.IStreamingClient MockMessageClient { get; set; }

        /// <summary>
        /// Initializes the isntance of <see cref="IStreamingServerAsync" /> implementation
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
        /// Initializes the instance of <see cref="IStreamingClientAsync"/> implementation
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

            MockMessageClient = new StreamingNamedPipeClient(serverUri);
        }


        /// <summary>
        /// Copies the provided source end point properties to the corresponding endpoind configured in this instance
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
        /// Copies the provided source endpoint properties to the corresponding receive endpoint configured in this instance
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
            MockMessageServer.Start();
        }

        /// <summary>
        /// Connects the client to the configured receiving endpoint
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ConnectClientAsync()
        {
            return await Task.Factory.StartNew(
                () => MockMessageClient
                    .Connect(TimeoutInSeconds * 1000)
                );
        }

        /// <summary>
        /// Receives a message from the messaging client
        /// </summary>
        /// <returns></returns>
        public async Task<MockMessage> ReceiveClientMessageAsync()
        {
            return await Task.Factory.StartNew(
                () => MockMessageClient.ReadMessage()
               );
        }

        /// <summary>
        /// Sends a message through the client instance
        /// </summary>
        /// <param name="mockMessage">The message that shall be sent through the client</param>
        /// <returns></returns>
        public async Task SendClientMessageAsync(MockMessage mockMessage)
        {
            await Task.Factory.StartNew(() =>
                MockMessageClient
                    .WriteMessage(mockMessage)
                );
        }

        /// <summary>
        /// Sends a message through the server instance 
        /// </summary>
        /// <param name="connectionId">The connection id to which the message shall be sent to</param>
        /// <param name="mockMessage">The message contents that shall be sent</param>
        /// <returns></returns>
        public async Task SendServerMessageAsync(int connectionId, MockMessage mockMessage)
        {
            await Task.Factory.StartNew(() =>            
                MockMessageServer
                    .WriteMessage(connectionId, mockMessage)
                    );
        }

        /// <summary>
        /// Receives a message from the messaging server instance
        /// </summary>
        /// <returns>An instance of <see cref="AsyncReadEventArgs"/> class containing the message that was received</returns>       
        public AsyncReadEventArgs ReceiveServerMessage()
        {   
            int connectionId = ConnectionId;
            System.Diagnostics.Trace.WriteLine(
                    $"MessageOperationHandler.GetNextMessage() invoked for URL: {Url}",
                    "TransMock.MessageOperationHandler");
            

            AsyncReadEventArgs receivedMessage = null;
            bool messageReceived = false;
            int retryCount = 0;

            while (!messageReceived)
            {
                // Now we wait for the reception of a message
                //bool waitElapsed = serverMessageReceivedEvent.Wait(TimeoutInSeconds * 1000);
                bool waitElapsed = receiveMessageSemaphore.Wait(TimeoutInSeconds * 1000);
                // Getting the connection Id right after exiting the wait
                connectionId = ConnectionId;
                
                System.Diagnostics.Debug.WriteLine(
                    $"MessageOperationHandler.GetNextMessage() exited the wait for endpoint with URL: {Url}, connection id: {connectionId}.",
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
            await Task.Factory.StartNew( 
                () => MockMessageClient
                    .Disconnect());
        }

        /// <summary>
        /// Clrears the messaging server instance.
        /// </summary>
        public void ClearServer()
        {
            if (MockMessageServer != null)
            {
                MockMessageServer.Stop();
                //var stopTask = MockMessageServer.Stop();
                //stopTask.Wait();
            }            
        }

        /// <summary>
        /// Internal implementation of the server instance initialization
        /// </summary>
        /// <param name="serverUri">The Uri of the server</param>
        private void InitMessagingServer(Uri serverUri)
        {
            MockMessageServer = new StreamingNamedPipeServer(
                   serverUri.AbsolutePath);

            MockMessageServer.ClientConnected += ClientConnected_EventHandler;
            MockMessageServer.ReadCompleted += MessageServer_ReadCompleted;
        }

        /// <summary>
        /// Event handler for new connections to the instance of <see cref="IStreamingServerAsync"/>
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="args">The event arguments</param>
        private void ClientConnected_EventHandler(object sender, ClientConnectedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(
                $@"MessageOperationConfig.ClientConnected_EventHandler() 
                    invoked for connection id: {args.ConnectionId}, URL: {Url}",
                "TransMock.TestMessagingClient");
            
        }

        /// <summary>
        /// Event handler for the ReadComplete event
        /// </summary>
        /// <param name="sender">the sender of the event</param>
        /// <param name="e">The event arcuments</param>
        private void MessageServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(
                $@"MessageOperationConfig.MessageServer_ReadCompleted() 
                    invoked for connection id: {e.ConnectionId}, URL: {Url} Active connectionId: {ConnectionId}",
                "TransMock.TestMessagingClient");

            
            while (!this.receivedMessages.TryAdd(e.ConnectionId, e))
            {
                System.Diagnostics.Debug.WriteLine(
                    "Message did not get added to the collection. Attempting again!",
                    "TransMock.TestMessagingClient");
            }

            ConnectionId = e.ConnectionId;
            receiveMessageSemaphore.Release();            

        }
    }
}