
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
/// Module      :  InverseMessagingClient.cs
/// Description :  This class implements the logic for communicating with mocked enpodings in an inverted manner.
/// -----------------------------------------------------------------------------------------------------------
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

namespace TransMock
{
    /// <summary>
    /// This class implements the logic for inverse communication with counterpart service endpoints represented by the <see cref="EndpointsMock"/> instance.
    /// This class is abstract and cannot be created directly but rather only by a factory method from the <see cref="EndpointsMock"/> isntance it is
    /// intended to be used against.
    /// Send operation from the this class corresponds to a receive endpoint in the related <see cref="EndpointsMock"/> instance.
    /// Receive operation in this class corresponds to a send endpoint in the related <see cref="EndpointsMock"/> instance.
    /// Same applies for 2-way communication.
    /// </summary>
    public abstract class InverseMessagingClient<TAddresses> where TAddresses : Addressing.EndpointAddress
    {
        private object syncRoot = new object();

        private EndpointsMock<TAddresses> endpointsMock;

        private TestContext testContext;

        private List<Task<InverseMessagingClient<TAddresses>>> parallelOperationsList;

        internal Dictionary<string, MessageOperationConfig> operationConfigurations;

        internal Queue<AsyncReadEventArgs> receivedMessagesQueue;

        protected InverseMessagingClient()
        {
            testContext = new TestContext();

            parallelOperationsList = new List<Task<InverseMessagingClient<TAddresses>>>(3);

            receivedMessagesQueue = new Queue<AsyncReadEventArgs>(3);

            operationConfigurations = new Dictionary<string, MessageOperationConfig>(3);
        }

        protected InverseMessagingClient(EndpointsMock<TAddresses> endpointsMock) : this()
        {
            this.endpointsMock = endpointsMock;
        }


        /// <summary>
        /// Wires up the mold with the integration mock
        /// </summary>
        /// <returns></returns>
        internal InverseMessagingClient<TAddresses> WireUp()
        {
            if (this.endpointsMock == null)
            {
                throw new InvalidOperationException("Endpoints mock instance not set!");
            }

            foreach (var mockedEndpoint in endpointsMock.endpointsMap.Values)
            {
                // Here is done the mirror mapping of integration endpoints                
                // to mocked endpoints
                if (mockedEndpoint is TwoWayReceiveEndpoint)
                {
                    // Integration two way receive endpoint is mapped to a mock two way send point
                    SetupSendRequestAndReceiveResponse(mockedEndpoint as TwoWayReceiveEndpoint);
                    continue;
                }

                if (mockedEndpoint is TwoWaySendEndpoint)
                {
                    // Integration two way send endpoint is mapped to a mock two way receive point
                    SetupReceiveRequestAndSendResponse(mockedEndpoint as TwoWaySendEndpoint);
                    continue;
                }

                if (mockedEndpoint is ReceiveEndpoint)
                {
                    // Integration receive endpoint is mapped to a mock send point
                    SetupSend(mockedEndpoint as ReceiveEndpoint);
                }

                if (mockedEndpoint is SendEndpoint)
                {
                    // Integration send endpoint is mapped to a mock receive point
                    SetupReceive(mockedEndpoint as SendEndpoint);
                }
                
            }

            return this;

        }

        /// <summary>
        /// Sets up a receive operation against a pre-defined service send endpoint
        /// </summary>
        /// <param name="sendEndpoint">The service send endpoint from which messages will be received</param>
        private void SetupReceive(SendEndpoint sendEndpoint)
        {
            if (this.operationConfigurations.ContainsKey(sendEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                return;
            }

            var receiveOperation = new MessageOperationConfig()
            {
                SendEndpoint = sendEndpoint,
                MockMessageServer = new StreamingNamedPipeServer(
                    new Uri(sendEndpoint.URL).AbsolutePath)
            };

            // We start the server as this is the whole point of the setup process
            // The mock should be ready to receive messages from the tested service
            // Before calling Start we need to hook the event handler for ReadCompleted
            // This brings some challanges with the chosen model, as the mock should then 
            // expose a public property exhibiting the queue that contains the received messages
            receiveOperation.MockMessageServer.ReadCompleted += MockMessageServer_ReadCompleted;
            receiveOperation.MockMessageServer.Start();


            // TODO: There should be added a check for uniqueness of the key
            operationConfigurations.Add(sendEndpoint.URL, receiveOperation);

        }

        /// <summary>
        /// Sets up a send operation against a service receive endpoint
        /// </summary>
        /// <param name="receiveEndpoint">The service receive endpoint to which messages shall be sent</param>
        private void SetupSend(ReceiveEndpoint receiveEndpoint)
        {
            if (this.operationConfigurations.ContainsKey(receiveEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                return;
            }

            var sendOperation = new MessageOperationConfig()
            {
                ReceiveEndpoint = receiveEndpoint,
                MockMessageClient = new StreamingNamedPipeClient(new System.Uri(receiveEndpoint.URL))
            };

            operationConfigurations.Add(receiveEndpoint.URL, sendOperation);

            return;

        }

        /// <summary>
        /// Sets up a 2/way receive operation for a corresponding 2-way service send endpoint
        /// </summary>
        /// <param name="sendReceiveEndpoint">The 2-way service send endpoint from which requests shall be received and responses sent to</param>
        private void SetupReceiveRequestAndSendResponse(TwoWaySendEndpoint sendReceiveEndpoint)
        {
            var receiveSendOperation = new MessageOperationConfig()
            {
                TwoWaySendEndpoint = sendReceiveEndpoint,
                MockMessageServer = new StreamingNamedPipeServer(
                    new Uri(sendReceiveEndpoint.URL).AbsolutePath)
            };

            receiveSendOperation.MockMessageServer.ReadCompleted += MockMessageServer_ReadCompleted;
            receiveSendOperation.MockMessageServer.Start();

            operationConfigurations.Add(sendReceiveEndpoint.URL, receiveSendOperation);

        }

        /// <summary>
        /// Sets up a 2-way send operation for corresponding 2-way service recieve endpoint
        /// </summary>
        /// <param name="receiveSendEndpoint">The 2-way service receive endpoing to whcih requests shall be sent and responses received from</param>
        private void SetupSendRequestAndReceiveResponse(TwoWayReceiveEndpoint receiveSendEndpoint)
        {
            var sendReceiveOperation = new MessageOperationConfig()
            {
                TwoWayReceiveEndpoint = receiveSendEndpoint,
                MockMessageClient = new StreamingNamedPipeClient(new System.Uri(receiveSendEndpoint.URL))
            };

            operationConfigurations.Add(receiveSendEndpoint.URL, sendReceiveOperation);

        }

        /// <summary>
        /// Tears down the operations that are set up for this mock isntance
        /// </summary>
        internal void TearDown()
        {
            // Stopping all the inbound message mock servers
            foreach (var item in this.operationConfigurations.Values)
            {
                if (item.MockMessageServer != null)
                {
                    item.MockMessageServer.Stop();
                }
            }
        }
        
        /// <summary>
        /// Event handler for whe messages are received on receive mock endpoints.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MockMessageServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            lock (this.syncRoot)
            {
                this.receivedMessagesQueue.Enqueue(e);

                System.Diagnostics.Debug.WriteLine(
                    "Message received in the MockMessageServer_ReadCompleted handler",
                    "TransMock.InverseMessagingClient");

                System.Threading.Monitor.Pulse(this.syncRoot);
            }
        }

        #region 1-way receive methods
        /// <summary>
        /// Receive messages from a corresponding 1-way service endpoint. Supports serially receive of multiple messages in the same instance
        /// </summary>
        /// <param name="sendAddress">The address of the service endpoint from which messages will be received</param>
        /// <param name="timeoutInSeconds">Timeout in seconds for waiting for connection from the send endpoint. Default is 10.</param>
        /// <param name="expectedMessageCount">The number of expected messages from the service send endpoint. Default is 1.</param>
        /// <param name="messageEncoding">The expected encoding of the messages contents. Default is UTF-8</param>
        /// <param name="contextAction">An action against the test context. Optional.</param>
        /// <param name="validator">A function performing validation logic on a single received message. Optional</param>
        /// <returns>The current instanse of the InverseMessagingClient</returns>
        public InverseMessagingClient<TAddresses> Receive(
            Expression<Func<TAddresses, Addressing.OneWaySendAddress>> sendAddress,
            int timeoutInSeconds=10,
            int expectedMessageCount=1,
            System.Text.Encoding messageEncoding=null,
            Action<TestContext> contextAction=null,
            Func<int, MockMessage, bool> validator=null)
        {

            return ReceiveImplementation((c, a) =>
            {
                var address = sendAddress.Compile()(a);

                var endpoindConfig = new SendEndpoint()
                {
                    URL = address.Value,
                    TimeoutInSeconds = timeoutInSeconds,
                    MessageEncoding = messageEncoding ?? System.Text.Encoding.UTF8,
                    ExpectedMessageCount = expectedMessageCount
                };

                contextAction?.Invoke(c);

                return endpoindConfig;
            },
            validator);
        }
        #endregion

        #region 1-way send methods
        /// <summary>
        /// Sends a one-way message to a corresponding receive endpoint represented by its receiving address
        /// </summary>
        /// <param name="receivingAddress">The address of the one way receiving end point that will receive the message being sent</param>
        /// <param name="requestFile">The path to the file containing the request</param>
        /// <param name="messageEncoding">The encoding of the message contents.Default is UTF-8</param>
        /// <param name="timeoutInSeconds">Timeout in seconds to wait for connection to the receive endpoint.Default is 10</param>
        /// <param name="messagePropertiesSetter">An optional function for setting message properties for promotion</param>
        /// <param name="contextAction">An optional action on the test context</param>
        /// <returns>The current instanse of the InverseMessagingClient</returns>
        public InverseMessagingClient<TAddresses> Send(
           Expression<Func<TAddresses, Addressing.OneWayReceiveAddress>> receivingAddress,
           string requestFile,           
           System.Text.Encoding messageEncoding=null,
           int timeoutInSeconds=10,
           Func<Dictionary<string, string>, Dictionary<string, string>> messagePropertiesSetter = null,
           Action<TestContext> contextAction=null)
        {
            return this.SendImplementation((c, a) =>
            {
                var address = receivingAddress.Compile()(a);

                //TODO: Lookup in the dictionary of endpoints for the corresponding receive one
                var receiveEndpoint = new ReceiveEndpoint()
                {
                    URL = address.Value,
                    RequestFilePath = requestFile,
                    MessageEncoding = messageEncoding ?? System.Text.Encoding.UTF8,
                    TimeoutInSeconds = timeoutInSeconds
                };

                contextAction?.Invoke(c);

                return receiveEndpoint;
            },
            messagePropertiesSetter);
        }
        #endregion

        #region 2-way receive methdos
        /// <summary>
        /// Receives a request from an endpoint with the specified senderAddress instance, optionally validates the request 
        /// and sends then a response as per the defined response selector method
        /// </summary>
        /// <param name="senderAddress">The corresponding 2-way sending endpoint address</param>
        /// <param name="responseSelector">The desired response selector function</param>
        /// <param name="timeoutInSeconds">Timeout in seconds. Default is 30</param>
        /// <param name="expectedMessageCount">Expected message count. Default is 1</param>
        /// <param name="messageEncoding">Expected request and ersponse messages encoding. Default is UTF-8</param>
        /// <param name="contextAction">Action for performing operations against the test context</param>
        /// <param name="requestValidator">Function for performing validation logic on the request MocMessage instance</param>
        /// <returns>The current instanse of the InverseMessagingClient</returns>
        public InverseMessagingClient<TAddresses> ReceiveRequestAndSendResponse(
            Expression<Func<TAddresses, Addressing.TwoWaySendAddress>> senderAddress,
            Func<MockMessage, ResponseSelectionStrategy> responseSelector,
            int timeoutInSeconds = 30,
            int expectedMessageCount = 1,
            System.Text.Encoding messageEncoding = null,
            Action<TestContext> contextAction = null,
            Func<int, MockMessage, bool> requestValidator = null)
        {
            return ReceiveImplementation((c, a) =>
                {
                    var address = senderAddress.Compile()(a);

                    var endpoindConfig = new SendEndpoint()
                    {
                        URL = address.Value,
                        TimeoutInSeconds = timeoutInSeconds,
                        MessageEncoding = messageEncoding ?? Encoding.UTF8,
                        ExpectedMessageCount = expectedMessageCount
                    };

                    contextAction?.Invoke(c);

                    return endpoindConfig;
                },
                requestValidator,
                responseSelector,
                this.SendResponse);
        }
        #endregion

        #region 2-Way Send implementation
        /// <summary>
        /// Sends a request and receives a response from a corresponding 2-way service receive endpoint represented by its address
        /// </summary>
        /// <param name="receivingAddress">The address of the corresponding 2-way service receive location to where requests will be sent and responses recieved</param>
        /// <param name="requestFilePath">The path to the file that contains the request contents</param>
        /// <param name="messageEncoding">The encodings of the request and send messages. Default is UTF-8</param>
        /// <param name="timeoutInSeconds">Timeout is seconds for waiting for connection to the recieve endpoint.Default is 10.</param>
        /// <param name="contextAction">An optional action to be performed on the test context</param>
        /// <param name="responseValidator">An optional response validator</param>
        /// <returns>The current instanse of the InverseMessagingClient</returns>
        public InverseMessagingClient<TAddresses> SendRequestAndReceiveResponse(
           Expression<Func<TAddresses, Addressing.TwoWayReceiveAddress>> receivingAddress,
           string requestFilePath,
           System.Text.Encoding messageEncoding=null,
           int timeoutInSeconds=10,
           Func<Dictionary<string, string>, Dictionary<string, string>> messagePropertiesSetter = null,
           Action<TestContext> contextAction=null,
           Func<MockMessage, bool> responseValidator=null)
        {
            return this.SendImplementation((c, a) =>
                {
                    var address = receivingAddress.Compile()(a);

                    //TODO: Lookup in the dictionary of endpoints for the corresponding receive one
                    var receiveEndpoint = new ReceiveEndpoint()
                    {
                        URL = address.Value,
                        RequestFilePath = requestFilePath,
                        MessageEncoding = messageEncoding ?? System.Text.Encoding.UTF8,
                        TimeoutInSeconds = timeoutInSeconds
                    };

                    contextAction?.Invoke(c);

                    return receiveEndpoint;
                },
                messagePropertiesSetter,
                responseValidator,
                this.ReceiveResponse
            );            
        }
        #endregion

        /// <summary>
        /// Configures mock operations to be executed in parallel to the main test execution thread.
        /// Each configured operation is executed in a separate task.
        /// </summary>
        /// <param name="parallelActions">A params array of send and receive operations to be executed in parallel</param>
        /// <returns>The current instanse of the InverseMessagingClient</returns>
        public InverseMessagingClient<TAddresses> InParallel(params Func<InverseMessagingClient<TAddresses>, InverseMessagingClient<TAddresses>>[] parallelActions)
        {
            //var actionsList = parallelActions(new List<TestMold<TAddresses>>());

            foreach (var action in parallelActions)
            {
                var task = new Task<InverseMessagingClient<TAddresses>>(
                    () =>
                    {
                        return action(this);
                    }
                );

                task.Start();

                parallelOperationsList.Add(task);
            }

            return this;
        }

        /// <summary>
        /// Validates that all configured parallel operations have completed.
        /// In case some experienced an error the corresponding exception will be re-thrown 
        /// in the main execution thread
        /// </summary>
        public void ValidateParallel()
        {
            try
            {
                // Cleanup any parallel tasks configured
                foreach (var operation in parallelOperationsList)
                {
                    if (!operation.IsCompleted)
                    {
                        operation.Wait();
                    }

                    if (operation.IsFaulted)
                    {
                        throw operation.Exception.InnerException;
                    }
                }
            }
            finally
            {
                // Clear down the casting mock
                this.TearDown();

                // Clearing the list
                parallelOperationsList.Clear();
            }
        }

        /// <summary>
        /// This is the implementation method of the Receive operation
        /// </summary>
        /// <param name="sender">A function implementing the logic for configuring the send point</param>
        /// <param name="validator">A function for performing validation logic on the received message</param>
        /// <param name="responseSelector">A function for performing response selection logic. Default is null for one way receives</param>
        /// <param name="responseSender">A function for performing response send logic. Default is null for one-way receives</param>
        /// <returns>The current instanse of the InverseMessagingClient</returns>
        private InverseMessagingClient<TAddresses> ReceiveImplementation(
            Func<TestContext, TAddresses, SendEndpoint> sender,
            Func<int, MockMessage, bool> validator,
            Func<MockMessage, ResponseSelectionStrategy> responseSelector = null,
            Action<MessageOperationConfig, ResponseSelectionStrategy, int,int,MockMessage> responseSender = null)
        {
            var sendEndpoint = sender(this.testContext, this.endpointsMock.mockAddresses);

            var endpointSetup = this.operationConfigurations
                .Where(kvp => kvp.Key == sendEndpoint.URL)
                .FirstOrDefault().Value;

            if (endpointSetup == null)
            {
                throw new InvalidOperationException("No corresponding endpoint setup found");
            }

            lock (this.syncRoot)
            {
                // Covering debatching scenarios
                for (int i = 0; i < sendEndpoint.ExpectedMessageCount; i++)
                {
                    // Now we wait for the reception of a message
                    bool waitElapsed = System.Threading.Monitor
                        .Wait(
                            this.syncRoot,
                            sendEndpoint.TimeoutInSeconds * 1000);

                    if (!waitElapsed)
                    {
                        throw new TimeoutException("No message received for the wait time set.");
                    }

                    // Now we read the message from the message queue

                    var receivedMessage = this.receivedMessagesQueue.Dequeue();

                    // Invoking the validator method
                    validator(i, receivedMessage.Message);

                    if (responseSender != null)
                    {
                        if (responseSelector == null)
                        {
                            throw new InvalidOperationException("No response selector defined");
                        }

                        var responseStrategy = responseSelector(receivedMessage.Message);
                        
                        // In case a response delegate is defined we call it too
                        responseSender(endpointSetup,
                            responseStrategy,
                            receivedMessage.ConnectionId,
                            i,
                            receivedMessage.Message);
                    }
                }

            }

            return this;
        }

        /// <summary>
        /// Implements the logic for sending a message to a corespongind receive endpoint and optionally receiving a response
        /// </summary>
        /// <param name="receiver">A function performing logic for configuring the corresponding receive endpoint details</param>
        /// <param name="messagePropertiesSetter">A function that receives an empty dictionary and returns is populated with 
        /// message properties for promotion</param>
        /// <param name="validator">A optional function performing validation on the received response in a 2/way scenario</param>
        /// <param name="responseReceiver">An optional function that receives a response in a 2-way scenario</param>
        /// <returns></returns>
        private InverseMessagingClient<TAddresses> SendImplementation(
            Func<TestContext, TAddresses, ReceiveEndpoint> receiver,
            Func<Dictionary<string,string>, Dictionary<string, string>> messagePropertiesSetter = null,
            Func<MockMessage, bool> validator = null,
            Func<MessageOperationConfig, MockMessage> responseReceiver = null)                
        {
            // We fetch first the actual receiver endpoint
            var receiverEndpoint = receiver(this.testContext, this.endpointsMock.mockAddresses);

            var endpointSetup = this.operationConfigurations
                .Where(kvp => kvp.Key == receiverEndpoint.URL)
                .FirstOrDefault().Value;

            if (endpointSetup == null)
            {
                throw new InvalidOperationException("No corresponding endpoint setup found");
            }

            try
            {
                endpointSetup.MockMessageClient
                    .Connect(receiverEndpoint.TimeoutInSeconds * 1000);
                                
                var mockMessage = new MockMessage(
                    receiverEndpoint.RequestFilePath,
                    receiverEndpoint.MessageEncoding);

                Dictionary<string, string> messageProperties =
                    new Dictionary<string, string>(3);

                mockMessage.Properties = messagePropertiesSetter?
                    .Invoke(messageProperties);                

                endpointSetup.MockMessageClient
                    .WriteMessage(mockMessage);

                if (responseReceiver != null)
                {
                    var responseMessage = responseReceiver(endpointSetup);

                    // Finally we validate the received response message
                    validator?.Invoke(responseMessage);
                }
            }
            finally
            {
                endpointSetup.MockMessageClient
                    .Disconnect();
            }

            return this;
        }

        /// <summary>
        /// Internal implementaion of the response send logic
        /// </summary>
        /// <param name="endpointSetup">The instance of the endpoint configuration for the receiving mock endpoint</param>
        /// <param name="responseStrategy">The instance of the <see cref="ResponseSelectionStrategy" /> class for providing the desired response contents </param>
        /// <param name="connectionId">The Id of the connection on which the response shall be provided</param>
        /// <param name="requestIndex">The index of the received request, that is used in the <see cref="ResponseSelectionStrategy" /> instance for selecting the correct response</param>
        /// <param name="requestMessage">The request message, that is used in the <see cref="ResponseSelectionStrategy" /> instance for selecting the correct response<</param>
        private void SendResponse(
            MessageOperationConfig endpointSetup, 
            ResponseSelectionStrategy responseStrategy,             
            int connectionId,
            int requestIndex,
            MockMessage requestMessage)
        {
            
            // Fetch the message based on the configured strategy
            var responseMessage = responseStrategy.SelectResponseMessage(
                requestIndex,
                requestMessage);            

            endpointSetup.MockMessageServer
                .WriteMessage(
                    connectionId,
                    responseMessage);

        }

        /// <summary>
        /// Internal implementatino of the receive response logic from a corersponding 2-way service receive endpoint
        /// </summary>
        /// <returns>An instance of the <see cref="MockMessage"/> class representing the received response</returns>
        private MockMessage ReceiveResponse(MessageOperationConfig endpointSetup)
        {
            // We receive the response
            var responseMessage = endpointSetup.MockMessageClient
                .ReadMessage();

            return responseMessage;
        }      
    }
}
