﻿
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
/// Module      :  TestMessagingClient.cs
/// Description :  This class implements the logic for communicating with mocked enpodings in an inverted manner.
/// -----------------------------------------------------------------------------------------------------------
/// 
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

namespace TransMock
{
    /// <summary>
    /// This class implements the logic for exchanging test messages with the counterpart service endpoints represented by a <see cref="EndpointsMock"/> instance.
    /// This class is abstract and cannot be created directly but rather through a factory method from the <see cref="EndpointsMock"/> isntance it is
    /// intended to be used against. The communication with the enpoint mocks is based on inversed messaging pattern:
    /// Send operation from the this class corresponds to a receive endpoint in the related <see cref="EndpointsMock"/> instance.
    /// Receive operation in this class corresponds to a send endpoint in the related <see cref="EndpointsMock"/> instance.
    /// Same applies for 2-way communication.
    /// </summary>
    public abstract class TestMessagingClient<TAddresses> where TAddresses : Addressing.EndpointAddress
    {
        private object sendSyncRoot = new object();

        private object receiveSyncRoot = new object();

        private static System.Threading.ManualResetEventSlim syncEvent;

        private EndpointsMock<TAddresses> endpointsMock;

        private TestContext testContext;        

        private IList<Task> parallelControllers;

        internal IDictionary<string, MessageOperationConfig> operationConfigurations;

        internal ConcurrentDictionary<int,AsyncReadEventArgs> receivedMessagesDictionary;

        /// <summary>
        /// Creates a default instance of the <see cref="TestMessagingClient{TAddresses}"/>
        /// </summary>
        protected TestMessagingClient()
        {
            testContext = new TestContext();

            syncEvent = new System.Threading.ManualResetEventSlim(false);

            parallelControllers = new List<Task>(3);

            receivedMessagesDictionary = new ConcurrentDictionary<int, AsyncReadEventArgs>();

            operationConfigurations = new Dictionary<string, MessageOperationConfig>(3);
       
        }

        /// <summary>
        /// Creates an instance of the <see cref="TestMessagingClient{TAddresses}"/> class for the provided <see cref="EndpointsMock{TAddresses}"/> instance
        /// </summary>
        /// <param name="endpointsMock"></param>
        protected TestMessagingClient(EndpointsMock<TAddresses> endpointsMock) : this()
        {
            this.endpointsMock = endpointsMock;
        }

        /// <summary>
        /// Wires up this client to the endpoints of the corresponding <see cref="EndpointsMock"/> instance
        /// </summary>
        /// <returns></returns>
        internal TestMessagingClient<TAddresses> WireUp()
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
            System.Diagnostics.Trace.WriteLine(
                "TestMessagingClient.SetupReceive() called for send endpoint with URL: " + sendEndpoint.URL,
                "TransMock.TestMessagingClient");

            if (this.operationConfigurations.ContainsKey(sendEndpoint.URL))
            {
                // We have a receive mock endpoint set for this service endpoint
                // so we exit gracefully
                System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.SetupReceive() found existing endpoint for URL: " + sendEndpoint.URL,
                    "TransMock.TestMessagingClient");

                return;
            }

            var receiveOperation = new MessageOperationConfig()
            {
                SendEndpoint = sendEndpoint,
                MockMessageServer = new StreamingNamedPipeServerAsync(
                    new Uri(sendEndpoint.URL).AbsolutePath)
            };

            // We start the server as this is the whole point of the setup process
            // The mock should be ready to receive messages from the tested service
            // Before calling Start we need to hook the event handler for ReadCompleted
            // This brings some challanges with the chosen model, as the mock should then 
            // expose a public property exhibiting the queue that contains the received messages
            receiveOperation.MockMessageServer.ReadCompleted += MockMessageServer_ReadCompleted;
            receiveOperation.MockMessageServer.StartAsync().ConfigureAwait(false);

            System.Diagnostics.Trace.WriteLine(
                "TestMessagingClient.SetupReceive() mock endpoint started listening for URL: " + sendEndpoint.URL,
                "TransMock.TestMessagingClient");

            // TODO: There should be added a check for uniqueness of the key
            operationConfigurations.Add(sendEndpoint.URL, receiveOperation);

            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.SetupReceive() addaded to the list mock endpoint for URL: " + sendEndpoint.URL,
                    "TransMock.TestMessagingClient");

        }

        /// <summary>
        /// Sets up a send operation against a service receive endpoint
        /// </summary>
        /// <param name="receiveEndpoint">The service receive endpoint to which messages shall be sent</param>
        private void SetupSend(ReceiveEndpoint receiveEndpoint)
        {
            System.Diagnostics.Trace.WriteLine(
                "TestMessagingClient.SetupSend() called for receive endpoint with URL: " + receiveEndpoint.URL,
                "TransMock.TestMessagingClient");

            if (this.operationConfigurations.ContainsKey(receiveEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.SetupSend() found existing endpoint for URL: " + receiveEndpoint.URL,
                    "TransMock.TestMessagingClient");

                return;
            }

            var sendOperation = new MessageOperationConfig()
            {
                ReceiveEndpoint = receiveEndpoint,
                MockMessageClient = new StreamingNamedPipeClientAsync(
                    new System.Uri(receiveEndpoint.URL))
            };

            operationConfigurations.Add(receiveEndpoint.URL, sendOperation);

            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.SetupSend() addaded to the list mock endpoint for URL: " + receiveEndpoint.URL,
                    "TransMock.TestMessagingClient");

            return;

        }

        /// <summary>
        /// Sets up a 2-way receive operation for a corresponding 2-way service send endpoint
        /// </summary>
        /// <param name="sendReceiveEndpoint">The 2-way service send endpoint from which requests shall be received and responses sent to</param>
        private void SetupReceiveRequestAndSendResponse(TwoWaySendEndpoint sendReceiveEndpoint)
        {
            System.Diagnostics.Trace.WriteLine(
                "TestMessagingClient.SetupReceiveRequestAndSendResponse() called for 2-way send endpoint with URL: " + sendReceiveEndpoint.URL,
                "TransMock.TestMessagingClient");

            if (this.operationConfigurations.ContainsKey(sendReceiveEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.SetupReceiveRequestAndSendResponse() found existing endpoint for URL: " + sendReceiveEndpoint.URL,
                    "TransMock.TestMessagingClient");

                return;
            }

            var receiveSendOperation = new MessageOperationConfig()
            {
                TwoWaySendEndpoint = sendReceiveEndpoint,
                MockMessageServer = new StreamingNamedPipeServerAsync(
                    new Uri(sendReceiveEndpoint.URL).AbsolutePath)
            };

            receiveSendOperation.MockMessageServer.ReadCompleted += MockMessageServer_ReadCompleted;
            receiveSendOperation.MockMessageServer.StartAsync().ConfigureAwait(false);

            System.Diagnostics.Trace.WriteLine(
                "TestMessagingClient.SetupReceiveRequestAndSendResponse() mock endpoint started listening for URL: " + sendReceiveEndpoint.URL,
                "TransMock.TestMessagingClient");

            operationConfigurations.Add(sendReceiveEndpoint.URL, receiveSendOperation);

            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.SetupReceiveRequestAndSendResponse() addaded to the list mock endpoint for URL: " + sendReceiveEndpoint.URL,
                    "TransMock.TestMessagingClient");

        }

        /// <summary>
        /// Sets up a 2-way send operation for corresponding 2-way service recieve endpoint
        /// </summary>
        /// <param name="receiveSendEndpoint">The 2-way service receive endpoing to whcih requests shall be sent and responses received from</param>
        private void SetupSendRequestAndReceiveResponse(TwoWayReceiveEndpoint receiveSendEndpoint)
        {
            System.Diagnostics.Trace.WriteLine(
                "TestMessagingClient.SetupSendRequestAndReceiveResponse() called for 2-way send endpoint with URL: " + receiveSendEndpoint.URL,
                "TransMock.TestMessagingClient");

            if (this.operationConfigurations.ContainsKey(receiveSendEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.SetupSendRequestAndReceiveResponse() found existing endpoint for URL: " + receiveSendEndpoint.URL,
                    "TransMock.TestMessagingClient");

                return;
            }

            var sendReceiveOperation = new MessageOperationConfig()
            {
                TwoWayReceiveEndpoint = receiveSendEndpoint,
                MockMessageClient = new StreamingNamedPipeClientAsync(
                    new System.Uri(receiveSendEndpoint.URL))
            };

            operationConfigurations.Add(receiveSendEndpoint.URL, sendReceiveOperation);

            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.SetupSendRequestAndReceiveResponse() addaded to the list mock endpoint for URL: " + receiveSendEndpoint.URL,
                    "TransMock.TestMessagingClient");

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
                    var stopTask = item.MockMessageServer.StopAsync();
                    stopTask.Wait();
                }
            }
        }
        
        /// <summary>
        /// Event handler for receiving messages by mocked receive endpoints.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">An instance of <see cref="AsyncReadEventArgs"/> class containing the details of the receive operation</param>
        private void MockMessageServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Message received in the MockMessageServer_ReadCompleted handler for connection id: {e.ConnectionId}",
                "TransMock.TestMessagingClient");

            while (!this.receivedMessagesDictionary.TryAdd(e.ConnectionId, e))
            {
                System.Diagnostics.Debug.WriteLine(
                    "Message did not get added to the collection. Attempting again!",
                    "TransMock.TestMessagingClient");
            }
            
            // Releasing the same number of threads as the number of messages in the collection
            // This is especially usefull for handling situations where multiple
            // receive operations are being processed
            syncEvent.Set();
        }

        #region 1-way receive methods
        /// <summary>
        /// Receive messages from a corresponding 1-way service endpoint. Supports serially receive of multiple messages in the same instance
        /// </summary>
        /// <param name="sendAddress">The address of the service endpoint from which messages will be received</param>
        /// <param name="timeoutInSeconds">Timeout in seconds for waiting for connection from the send endpoint. Default is 10.</param>
        /// <param name="expectedMessageCount">The number of expected messages from the service send endpoint. Default is 1.</param>
        /// <param name="messageEncoding">The expected encoding of the messages contents. Default is UTF-8</param>
        /// <param name="beforeReceiveAction">An action against the test context. Optional.</param>
        /// <param name="validator">A function performing validation logic on a single received message. Optional</param>
        /// <returns>The current instanse of the TestMessagingClient</returns>
        public TestMessagingClient<TAddresses> Receive(
            Expression<Func<TAddresses, Addressing.OneWaySendAddress>> sendAddress,
            int timeoutInSeconds=10,
            int expectedMessageCount=1,
            System.Text.Encoding messageEncoding=null,
            Action<TestContext> beforeReceiveAction=null,
            Action<TestContext> afterReceiveAction = null,
            Func<IndexedMessageReception, bool> validator=null)
        {
            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.Receive() called",
                    "TransMock.TestMessagingClient");

            var receiveTask = ReceiveImplementationAsync((c, a) =>
            {
                var address = sendAddress.Compile()(a);

                var endpoindConfig = new SendEndpoint()
                {
                    URL = address.Value,
                    TimeoutInSeconds = timeoutInSeconds,
                    MessageEncoding = messageEncoding ?? System.Text.Encoding.UTF8,
                    ExpectedMessageCount = expectedMessageCount
                };

                beforeReceiveAction?.Invoke(c);

                return endpoindConfig;
            },
            validator,
            afterReceiveAction: afterReceiveAction);

            receiveTask.Wait();

            return receiveTask.Result;

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
        /// <param name="messagePropertiesSetter">An optional action for setting message properties for promotion</param>
        /// <param name="beforeSendAction">An optional action on the test context before sending a message</param>
        /// <param name="afterSendAction">An optional action on the test context after a message was sent</param>
        /// <returns>The current instanse of the TestMessagingClient</returns>
        public TestMessagingClient<TAddresses> Send(
           Expression<Func<TAddresses, Addressing.OneWayReceiveAddress>> receivingAddress,
           string requestFile,           
           System.Text.Encoding messageEncoding=null,
           int timeoutInSeconds=10,
           Action<IDictionary<string, string>> messagePropertiesSetter = null,
           Action<TestContext> beforeSendAction=null,
           Action<TestContext> afterSendAction = null)
        {
            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.Send() called",
                    "TransMock.TestMessagingClient");

            var sendTask = this.SendImplementationAsync((c, a) =>
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

                beforeSendAction?.Invoke(c);

                return receiveEndpoint;
            },
            messagePropertiesSetter,
            afterSendAction: afterSendAction);

            sendTask.Wait();

            return sendTask.Result;
        }
        #endregion

        #region 2-way receive methdos
        /// <summary>
        /// Receives a request from an endpoint with the specified senderAddress instance, optionally validates the request 
        /// and sends then a response as per the defined response selector method
        /// </summary>
        /// <param name="senderAddress">The corresponding 2-way sending endpoint address</param>
        /// <param name="responseSelector">The desired response selector function</param>
        /// <param name="responsePropertiesSetter">Function for setting message context properties in the response message</param>
        /// <param name="timeoutInSeconds">Timeout in seconds. Default is 30</param>
        /// <param name="expectedMessageCount">Expected message count. Default is 1</param>
        /// <param name="messageEncoding">Expected request and ersponse messages encoding. Default is UTF-8</param>
        /// <param name="beforeRequestAction">Action for performing operations against the test context</param>
        /// <param name="requestValidator">Function for performing validation logic on the request MocMessage instance</param>
        /// <param name="afterRequestAction">An optional action that can be performed after the request was sent</param>
        /// <param name="afterResponseAction">An optional action that can be performed after the reresponse was received</param>
        /// <returns>The current instanse of the TestMessagingClient</returns>
        public TestMessagingClient<TAddresses> ReceiveRequestAndSendResponse(
            Expression<Func<TAddresses, Addressing.TwoWaySendAddress>> senderAddress,
            Func<MockMessage, ResponseSelectionStrategy> responseSelector,
            Action<IDictionary<string, string>> responsePropertiesSetter = null,
            int timeoutInSeconds = 30,
            int expectedMessageCount = 1,
            System.Text.Encoding messageEncoding = null,
            Action<TestContext> beforeRequestAction = null,
            Func<IndexedMessageReception, bool> requestValidator = null,
            Action<TestContext> afterRequestAction = null,
            Action<TestContext> afterResponseAction = null)
        {
            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.ReceiveRequestAndSendResponse() called",
                    "TransMock.TestMessagingClient");

            var receiveSendTask = ReceiveImplementationAsync((c, a) =>
                {
                    var address = senderAddress.Compile()(a);

                    var endpoindConfig = new SendEndpoint()
                    {
                        URL = address.Value,
                        TimeoutInSeconds = timeoutInSeconds,
                        MessageEncoding = messageEncoding ?? Encoding.UTF8,
                        ExpectedMessageCount = expectedMessageCount
                    };

                    beforeRequestAction?.Invoke(c);

                    return endpoindConfig;
                },
                requestValidator,
                responseSelector,
                responsePropertiesSetter,
                this.SendResponseAsync,
                afterRequestAction,
                afterResponseAction);

            receiveSendTask.Wait();

            return receiveSendTask.Result;
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
        /// <param name="messagePropertiesSetter">An optional action for setting custom message properties in the request message</param>
        /// <param name="beforeRequestAction">An optional action to be performed on the test context</param>
        /// <param name="responseValidator">An optional response validator</param>
        /// <param name="afterRequestAction">An optional action that can be performed after the request was sent</param>
        /// <param name="afterResponseAction">An optional action that can be performed after the reresponse was received</param>
        /// <returns>The current instanse of the TestMessagingClient</returns>
        public TestMessagingClient<TAddresses> SendRequestAndReceiveResponse(
           Expression<Func<TAddresses, Addressing.TwoWayReceiveAddress>> receivingAddress,
           string requestFilePath,
           System.Text.Encoding messageEncoding=null,
           int timeoutInSeconds=10,
           Action<IDictionary<string, string>> messagePropertiesSetter = null,
           Action<TestContext> beforeRequestAction=null,
           Func<MockMessage, bool> responseValidator=null,
           Action<TestContext> afterRequestAction = null,
           Action<TestContext> afterResponseAction = null)
        {
            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.SendRequestAndReceiveResponse() called",
                    "TransMock.TestMessagingClient");

            var sendReceiveTask = this.SendImplementationAsync((c, a) =>
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

                    beforeRequestAction?.Invoke(c);

                    return receiveEndpoint;
                },
                messagePropertiesSetter,
                responseValidator,
                this.ReceiveResponseAsync,
                afterRequestAction,
                afterResponseAction
            );

            sendReceiveTask.Wait();

            return sendReceiveTask.Result;         
        }
        #endregion

        #region Parallel processing methods
        /// <summary>
        /// Configures mock operations to be executed in parallel to the main test execution thread.
        /// Each configured operation is executed in a separate task.
        /// </summary>
        /// <param name="parallelActions">A params array of send and receive operations to be executed in parallel</param>
        /// <returns>The current instanse of the TestMessagingClient</returns>
        public TestMessagingClient<TAddresses> InParallel(params Func<TestMessagingClient<TAddresses>, TestMessagingClient<TAddresses>>[] parallelActions)
        {
            var tasks = new List<Task<TestMessagingClient<TAddresses>>>(parallelActions.Length);

            foreach (var action in parallelActions)
            {
                var task = new Task<TestMessagingClient<TAddresses>>(
                    () =>
                    {
                        return action(this);
                    }
                );

                task.Start();

                tasks.Add(task);
            }

            // Starting a controller task for the set of parallel operations
            var controllerTask = new Task(
                () =>
                {
                    try
                    {
                        Task.WaitAll(tasks.ToArray(), TimeSpan.FromMinutes(5));
                    }
                    catch(AggregateException aggException)
                    {
                        foreach (var ex in aggException.InnerExceptions)
                        {
                            LogException(ex);
                        }
                    }
                });

            controllerTask.Start();

            parallelControllers.Add(controllerTask);

            return this;
        }

        /// <summary>
        /// Verifies that all configured parallel operations have completed.
        /// In case some experienced an error the corresponding exception will be
        /// logged for further troubleshooting
        /// </summary>
        public void VerifyParallel()
        {
            try
            {
                // Wait for any parallel tasks to complete for a reasonable wait time
                if (parallelControllers.Count() > 0)
                {
                    if (!Task.WaitAll(
                            parallelControllers.ToArray(),
                            TimeSpan.FromMinutes(5)))
                    {
                        throw new TimeoutException(
                            "Waiting for the parallel operations to complete exceeded the allotted time!");
                    }
                }                
            }
            catch(AggregateException aggException)
            {
                foreach (var ex in aggException.InnerExceptions)
                {
                    LogException(ex);
                }
            }
            finally
            {
                // Clear down the casting mock
                this.TearDown();

                // Clearing the list
                parallelControllers.Clear();
            }
        }

        #endregion

        #region Private messaging implementation methods
        /// <summary>
        /// This is the implementation method of the Receive operation
        /// </summary>
        /// <param name="sender">A function implementing the logic for configuring the send point</param>
        /// <param name="validator">A function for performing validation logic on the received message</param>
        /// <param name="responseSelector">A function for performing response selection logic. Default is null for one way receives</param>
        /// <param name="responsePropertiesSetter">An optional action that allows for adding custom properties to the response message</param>
        /// <param name="responseSender">A function for performing response send logic. Default is null for one-way receives</param>
        /// <param name="afterReceiveAction">An action for performing an operation after the request is received</param>
        /// <param name="afterSendAction"></param>
        /// <returns>The current instanse of the TestMessagingClient</returns>
        private async Task<TestMessagingClient<TAddresses>> ReceiveImplementationAsync(
            Func<TestContext, TAddresses, SendEndpoint> sender,
            Func<IndexedMessageReception, bool> validator,
            Func<MockMessage, ResponseSelectionStrategy> responseSelector = null,
            Action<IDictionary<string, string>> responsePropertiesSetter = null,
            Func<MessageOperationConfig, ResponseSelectionStrategy, int,int,MockMessage, 
                Action<IDictionary<string, string>>, Task> responseSender = null,
            Action<TestContext> afterReceiveAction = null,
            Action<TestContext> afterSendAction = null)
        {
            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.ReceiveImplementationAsync() called",
                    "TransMock.TestMessagingClient");

            var sendEndpoint = sender(this.testContext, this.endpointsMock.mockAddresses);

            var endpointSetup = this.operationConfigurations
                .Where(kvp => kvp.Key == sendEndpoint.URL)
                .FirstOrDefault().Value;

            if (endpointSetup == null)
            {
                System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.ReceiveImplementationAsync() did not find endpoint setup for URL: " + sendEndpoint.URL,
                    "TransMock.TestMessagingClient");

                throw new InvalidOperationException("No corresponding endpoint setup found for URL" + sendEndpoint.URL);
            }

            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.ReceiveImplementationAsync() fetched endpoint setup for URL: " + sendEndpoint.URL,
                    "TransMock.TestMessagingClient");

            for (int i = 0; i < sendEndpoint.ExpectedMessageCount; i++)
            {
                    
                System.Diagnostics.Trace.WriteLine(
                    string.Format("TestMessagingClient.ReceiveImplementationAsync() waiting for message {0} from send endpoint with URL: {1}",
                        i,
                        sendEndpoint.URL),
                    "TransMock.TestMessagingClient");

                AsyncReadEventArgs receivedMessage = null;
                bool messageReceived = false;
                while (!messageReceived)
                {
                    // Now we wait for the reception of a message
                    bool waitElapsed = syncEvent.Wait(sendEndpoint.TimeoutInSeconds * 1000);

                    try
                    {
                        if (!waitElapsed)
                        {
                            Console.WriteLine(
                                "Message {0} from send endpoint with URL: {1} not received in allotted time",
                                i,
                                sendEndpoint.URL);

                            System.Diagnostics.Trace.WriteLine(
                                string.Format(@"TestMessagingClient.ReceiveImplementationAsync() 
                                    did not receive in time message {0} from send endpoint with URL: {1}",
                                    i,
                                    sendEndpoint.URL),
                                "TransMock.TestMessagingClient");

                            throw new TimeoutException("No message received for the wait time set.");
                        }
                    }
                    finally
                    {
                        // Releasing the semaphore
                        syncEvent.Reset();
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine("<<<<<<< Receiving message from send endpoint with URL: {0} <<<<<<<",
                        sendEndpoint.URL);
                    Console.WriteLine();

                    // Now we read the message from the message queue
                    
                    int connectionId = endpointSetup.MockMessageServer.ConnectionId; // This is the connection/server Id

                    messageReceived = this.receivedMessagesDictionary
                    .TryRemove(
                        connectionId,
                        out receivedMessage);

                    if (!messageReceived)
                    {
                        System.Diagnostics.Trace.WriteLine(
                        $"TestMessagingClient.ReceiveImplementationAsync() did not manage to fetch message from internal collection for send endpoint with URL: {sendEndpoint.URL} and connection Id: {connectionId}. Continuing to try!",
                        "TransMock.TestMessagingClient");
                    }
                }
                
                
                System.Diagnostics.Trace.WriteLine(
                    string.Format("TestMessagingClient.ReceiveImplementationAsync() received message {0} from send endpoint with URL: {1}",
                        i,
                        sendEndpoint.URL),
                    "TransMock.TestMessagingClient");

                    Console.WriteLine(
                        "Received message {0} out of {1} from send endpoint with URL: {2}",
                        i+1,
                        sendEndpoint.ExpectedMessageCount,
                        sendEndpoint.URL);

                    LogMessageContents(receivedMessage.Message);

                    System.Diagnostics.Trace.WriteLine(
                        string.Format("TestMessagingClient.ReceiveImplementationAsync() invoking validator for message {0} from send endpoint with URL: {1}",
                            i,
                            sendEndpoint.URL),
                        "TransMock.TestMessagingClient");

                    var validatableReception = new IndexedMessageReception()
                    {
                        Index = i,
                        Message = receivedMessage.Message
                    };

                    // Invoking the validator method
                    validator(validatableReception);

                    System.Diagnostics.Trace.WriteLine(
                        string.Format("TestMessagingClient.ReceiveImplementationAsync() invoking afterReceiveAction for message {0} from send endpoint with URL: {1}",
                            i,
                            sendEndpoint.URL),
                        "TransMock.TestMessagingClient");

                    // Invoking the after request reception action
                    afterReceiveAction?.Invoke(this.testContext);

                    System.Diagnostics.Trace.WriteLine(
                        string.Format("TestMessagingClient.ReceiveImplementationAsync() done with received message {0} from send endpoint with URL: {1}",
                            i,
                            sendEndpoint.URL),
                        "TransMock.TestMessagingClient");

                    // In case a response delegate is defined we call it too
                    if (responseSender != null)
                    {
                        Console.WriteLine(
                            ">>>>>>> Sending response for request message {0} from 2-way send endpoint with URL: {1}",
                            i+1,
                            sendEndpoint.URL);

                        System.Diagnostics.Trace.WriteLine(
                            string.Format("TestMessagingClient.ReceiveImplementationAsync() sending response for request message {0} from 2-way send endpoint with URL: {1}",
                                i,
                                sendEndpoint.URL),
                            "TransMock.TestMessagingClient");

                        if (responseSelector == null)
                        {
                            Console.WriteLine(
                                "No response strategy selector defined for message {0} from 2-way send endpoint with URL: {1}",
                                i+1,
                                sendEndpoint.URL);

                            throw new InvalidOperationException("No response selector defined for 2-way send endpoint with URL: " + sendEndpoint.URL);
                        }

                        var responseStrategy = responseSelector(receivedMessage.Message);

                        System.Diagnostics.Trace.WriteLine(
                            string.Format("TestMessagingClient.ReceiveImplementationAsync() response strategy set for request message {0} from 2-way send endpoint with URL: {1}",
                                i,
                                sendEndpoint.URL),
                            "TransMock.TestMessagingClient");
                        
                        await responseSender(endpointSetup,
                            responseStrategy,
                            receivedMessage.ConnectionId,
                            i,
                            receivedMessage.Message,
                            responsePropertiesSetter)
                                .ConfigureAwait(false);

                        System.Diagnostics.Trace.WriteLine(
                        string.Format("TestMessagingClient.ReceiveImplementationAsync() invoking afterSendAction for message {0} from send endpoint with URL: {1}",
                            i,
                            sendEndpoint.URL),
                        "TransMock.TestMessagingClient");

                        // Invoking the after response send action
                        afterSendAction?.Invoke(this.testContext);

                        System.Diagnostics.Trace.WriteLine(
                        string.Format("TestMessagingClient.ReceiveImplementationAsync() done with response for message {0} from send endpoint with URL: {1}",
                            i,
                            sendEndpoint.URL),
                        "TransMock.TestMessagingClient");

                        Console.WriteLine(">>>>>>> End of Sending response message to 2-way send endpoint >>>>>>>");
                    }

                    Console.WriteLine();
                    Console.WriteLine("<<<<<<< End of Receiving message from send endpoint <<<<<<<");
                    Console.WriteLine();
                }
            //}

            return this;
        }        

        /// <summary>
        /// Implements the logic for sending a message to a corespongind receive endpoint and optionally receiving a response
        /// </summary>
        /// <param name="receiver">A function performing logic for configuring the corresponding receive endpoint details</param>
        /// <param name="messagePropertiesSetter">An optional function that receives an empty dictionary and returns is populated with 
        /// message properties for promotion</param>
        /// <param name="validator">An optional function performing validation on the received response in a 2/way scenario</param>
        /// <param name="responseReceiverAsync">An optional function that receives a response in a 2-way scenario</param>
        /// <param name="afterSendAction">An optional action that can be performed after a message has beend sent</param>
        /// <param name="afterReceiveAction">An optional action that can be performed after a response message was received</param>
        /// <returns></returns>
        private async Task<TestMessagingClient<TAddresses>> SendImplementationAsync(
            Func<TestContext, TAddresses, ReceiveEndpoint> receiver,
            Action<IDictionary<string,string>> messagePropertiesSetter = null,
            Func<MockMessage, bool> validator = null,
            Func<MessageOperationConfig, Task<MockMessage>> responseReceiverAsync = null,
            Action<TestContext> afterSendAction = null,
            Action<TestContext> afterReceiveAction = null)                
        {
            System.Diagnostics.Trace.WriteLine(
                    "TestMessagingClient.SendImplementation() called",
                    "TransMock.TestMessagingClient");

            // We fetch first the actual receiver endpoint
            var receiverEndpoint = receiver(this.testContext, this.endpointsMock.mockAddresses);

            var endpointSetup = this.operationConfigurations
                .Where(kvp => kvp.Key == receiverEndpoint.URL)
                .FirstOrDefault().Value;

            if (endpointSetup == null)
            {
                System.Diagnostics.Trace.WriteLine(
                   "TestMessagingClient.SendImplementation() did not find endpoint setup for URL: " + receiverEndpoint.URL,
                   "TransMock.TestMessagingClient");

                throw new InvalidOperationException("No corresponding endpoint setup found");
            }

            System.Diagnostics.Trace.WriteLine(
                   "TestMessagingClient.SendImplementation() found endpoint setup for URL: " + receiverEndpoint.URL,
                   "TransMock.TestMessagingClient");

            try
            {
                  
                    System.Diagnostics.Trace.WriteLine(
                       "TestMessagingClient.SendImplementation() connecting endpoint setup for URL: " + receiverEndpoint.URL,
                       "TransMock.TestMessagingClient");

                    await endpointSetup.MockMessageClient
                        .ConnectAsync(receiverEndpoint.TimeoutInSeconds * 1000)
                        .ConfigureAwait(false);

                    System.Diagnostics.Debug.WriteLine(
                       string.Format(
                            "TestMessagingClient.SendImplementation() reading message contents from file {0} for receive endpoint with URL: {1}",
                            receiverEndpoint.RequestFilePath,
                            receiverEndpoint.URL),
                       "TransMock.TestMessagingClient");

                    var mockMessage = new MockMessage(
                        receiverEndpoint.RequestFilePath,
                        receiverEndpoint.MessageEncoding);

                    System.Diagnostics.Debug.WriteLine(
                       string.Format(
                            "TestMessagingClient.SendImplementation() setting up properties in request message for receive endpoint with URL: {0}",
                            receiverEndpoint.URL),
                       "TransMock.TestMessagingClient");

                    Dictionary<string, string> messageProperties =
                        new Dictionary<string, string>(3);

                    messagePropertiesSetter?
                        .Invoke(messageProperties);

                    if (messageProperties.Count() > 0)
                    {
                        mockMessage.Properties = messageProperties;
                    }

                    Console.WriteLine(
                        ">>>>>>> Sending message to receive endpoint with URL: {0} >>>>>>>",
                        receiverEndpoint.URL);

                    System.Diagnostics.Trace.WriteLine(
                       string.Format(
                            "TestMessagingClient.SendImplementation() sending message to receive endpoint with URL: {0}",
                            receiverEndpoint.URL),
                       "TransMock.TestMessagingClient");

                    LogMessageContents(mockMessage);

                    await endpointSetup.MockMessageClient
                        .WriteMessageAsync(mockMessage)
                        .ConfigureAwait(false);

                    System.Diagnostics.Trace.WriteLine(
                       string.Format(
                            "TestMessagingClient.SendImplementation() invoking afterSendAction for receive endpoint with URL: {0}",
                            receiverEndpoint.URL),
                       "TransMock.TestMessagingClient");

                    // Invoking the after send action
                    afterSendAction?.Invoke(this.testContext);

                    if (responseReceiverAsync != null)
                    {
                        Console.WriteLine(
                            "<<<<<<< Receiving response from 2-way receive endpoint with URL: {0}",
                            receiverEndpoint.URL);

                        endpointSetup.TwoWayReceiveEndpoint
                            .TimeoutInSeconds = receiverEndpoint.TimeoutInSeconds;
                        endpointSetup.TwoWayReceiveEndpoint
                            .MessageEncoding = receiverEndpoint.MessageEncoding;

                        System.Diagnostics.Trace.WriteLine(
                           string.Format(
                                "TestMessagingClient.SendImplementation() receiving response from 2-way receive endpoint with URL: {0}",
                                receiverEndpoint.URL),
                           "TransMock.TestMessagingClient");

                        var responseMessage = await responseReceiverAsync(endpointSetup);

                        LogMessageContents(responseMessage);

                        System.Diagnostics.Trace.WriteLine(
                           string.Format(
                                "TestMessagingClient.SendImplementation() invoking the response validator for 2-way receive endpoint with URL: {0}",
                                receiverEndpoint.URL),
                           "TransMock.TestMessagingClient");

                        // Finally we validate the received response message
                        validator?.Invoke(responseMessage);

                        System.Diagnostics.Trace.WriteLine(
                           string.Format(
                                "TestMessagingClient.SendImplementation() invoking afterReceiveAction for 2-way receive endpoint with URL: {0}",
                                receiverEndpoint.URL),
                           "TransMock.TestMessagingClient");

                        // Invoking the after receive action
                        afterReceiveAction?.Invoke(this.testContext);

                        System.Diagnostics.Trace.WriteLine(
                           string.Format(
                                "TestMessagingClient.SendImplementation() done with response from for 2-way receive endpoint with URL: {0}",
                                receiverEndpoint.URL),
                           "TransMock.TestMessagingClient");

                        Console.WriteLine();
                        Console.WriteLine("<<<<<<< End of Receiving response message from 2-way receive endpoint");
                        Console.WriteLine();
                    }

                    Console.WriteLine();
                    Console.WriteLine(">>>>>>> End of Sending message to receive endpoint >>>>>>>");
                    Console.WriteLine();
                //}
            }
            finally
            {
                System.Diagnostics.Trace.WriteLine(
                       string.Format(
                            "TestMessagingClient.SendImplementation() disconnecting for receive endpoint with URL: {0}",
                            receiverEndpoint.URL),
                       "TransMock.TestMessagingClient");

                await endpointSetup.MockMessageClient
                    .DisconnectAsync()
                    .ConfigureAwait(false);
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
        private async Task SendResponseAsync(
            MessageOperationConfig endpointSetup, 
            ResponseSelectionStrategy responseStrategy,             
            int connectionId,
            int requestIndex,
            MockMessage requestMessage,
            Action<IDictionary<string, string>> messagePropertiesSetter = null)
        {
            System.Diagnostics.Trace.WriteLine(
                string.Format("TestMessagingClient.SendResponse() sending response for request message {0} from 2-way send endpoint with URL: {1}",
                    requestIndex,
                    endpointSetup.TwoWaySendEndpoint.URL),
                "TransMock.TestMessagingClient");

            // Fetch the message based on the configured strategy
            var responseMessage = responseStrategy.SelectResponseMessage(
                requestIndex,
                requestMessage);

            System.Diagnostics.Trace.WriteLine(
                string.Format("TestMessagingClient.SendResponse() response message set for request message {0} from 2-way send endpoint with URL: {1}",
                    requestIndex,
                    endpointSetup.TwoWaySendEndpoint.URL),
                "TransMock.TestMessagingClient");

            Dictionary<string, string> messageProperties =
                        new Dictionary<string, string>(3);

            messagePropertiesSetter?
                        .Invoke(messageProperties);

            if (messageProperties.Count() > 0)
            {
                System.Diagnostics.Trace.WriteLine(
                    string.Format("TestMessagingClient.SendResponse() setting properties in response message for request message {0} from 2-way send endpoint with URL: {1}",
                        requestIndex,
                        endpointSetup.TwoWaySendEndpoint.URL),
                    "TransMock.TestMessagingClient");

                responseMessage.Properties = messageProperties;
            }

            LogMessageContents(responseMessage);

            await endpointSetup.MockMessageServer
                .WriteMessageAsync(
                    connectionId,
                    responseMessage);

            System.Diagnostics.Trace.WriteLine(
                string.Format("TestMessagingClient.SendResponse() sent response message for request message {0} from 2-way send endpoint with URL: {1}",
                    requestIndex,
                    endpointSetup.TwoWaySendEndpoint.URL),
                "TransMock.TestMessagingClient");

        }

        /// <summary>
        /// Internal implementatino of the receive response logic from a corersponding 2-way service receive endpoint
        /// </summary>
        /// <returns>An instance of the <see cref="MockMessage"/> class representing the received response</returns>
        private async Task<MockMessage> ReceiveResponseAsync(MessageOperationConfig endpointSetup)
        {
            // We receive the response in an async way
            // as we need to cancel the operation in case the timeout period is exceeded
            var responseReceptionTask = endpointSetup
                .MockMessageClient.ReadMessageAsync();

            var resultTask = await Task.WhenAny(
                responseReceptionTask,
                Task.Delay(
                    TimeSpan.FromSeconds(
                        endpointSetup.TwoWayReceiveEndpoint.TimeoutInSeconds)));

            if(resultTask == responseReceptionTask)
            {
                System.Diagnostics.Trace.WriteLine(
                        string.Format("TestMessagingClient.ReceiveResponse() received response from 2-way receive endpoint with URL: {0}",
                            endpointSetup.TwoWayReceiveEndpoint.URL),
                        "TransMock.TestMessagingClient");

                return responseReceptionTask.Result;
            }
            else
            {
                throw new TimeoutException(
                    string.Format("Response reception from 2-way receive endpoint with URL: {0} exceeded allotted wait time!",
                        endpointSetup.TwoWayReceiveEndpoint.URL));
            }          
        }
        #endregion

        #region Log methods
        /// <summary>
        /// Logs the message contents to the configured trace output
        /// </summary>
        /// <param name="message">The mock message instance to be logged</param>
        private void LogMessageContents(MockMessage message)
        {
            Console.WriteLine(
                "************** Message contents ****************");            

            if (message != null)
            {
                Console.WriteLine(
                    "### Message properties ###");

                foreach (var messageProperty in message.Properties)
                {
                    Console.WriteLine(
                        "\tName: {0}   Value: {1}",
                        messageProperty.Key,
                        messageProperty.Value);
                }

                Console.WriteLine(
                    "### End of Message properties ###");
                Console.WriteLine();

                Console.WriteLine(
                    "### Message body ###");

                Console.WriteLine(
                    message.Body);

                Console.WriteLine(
                    "### End of Message body ###");
            }

            Console.WriteLine(
                "************** End message contents ************");           
        }


        private void LogException(Exception ex)
        {
            Console.WriteLine(
               "!!!!!!!!!!!!!! Exception thrown !!!!!!!!!!!!!!");

            LogExceptionDetails(ex);

            Console.WriteLine(
               "!!!!!!!!!!!!!! End of Exception !!!!!!!!!!!!!!");

        }

        private void LogExceptionDetails(Exception ex, bool inner=false)
        {
            Console.WriteLine();

            if (inner)
            {
                Console.WriteLine("!!!!! Inner exception details !!!!!!");
            }

            Console.WriteLine($"Error message: {ex.Message}");
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");

            if (ex.InnerException != null)
                LogExceptionDetails(ex.InnerException, true);

        }
        #endregion

    }
}
