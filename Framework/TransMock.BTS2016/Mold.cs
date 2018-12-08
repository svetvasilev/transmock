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
    /// This class represent the actual mocked mold where the operations are mirrored compared to the
    /// actual integration represented by the TestCasting object. 
    /// Send operation from the mold isntance corresponds to a receive operation in the casting (integration)
    /// Receive operation in the mold corresponds to a send operation from the casting, and so on.
    /// </summary>
    public abstract class Mold<TAddresses> where TAddresses : class
    {
        private object syncRoot = new object();

        private IntegrationMock<TAddresses> casting;

        private TestContext testContext;

        private List<Task<Mold<TAddresses>>> parallelOperationsList;

        internal Dictionary<string, MessageOperationExpectation> operationExpectations;

        internal Queue<AsyncReadEventArgs> receivedMessagesQueue;

        protected Mold()
        {
            testContext = new TestContext();

            parallelOperationsList = new List<Task<Mold<TAddresses>>>(3);

            receivedMessagesQueue = new Queue<AsyncReadEventArgs>(3);

            operationExpectations = new Dictionary<string, MessageOperationExpectation>(3);
        }

        protected Mold(IntegrationMock<TAddresses> casting) : this()
        {
            this.casting = casting;
        }


        /// <summary>
        /// Wires up the mold with the integration mock
        /// </summary>
        /// <returns></returns>
        public Mold<TAddresses> WireUp()
        {
            if (this.casting == null)
            {
                throw new InvalidOperationException("Casting instance not set!");
            }

            foreach (var mockedEndpoint in casting.endpointsMap.Values)
            {
                // Here is done the mirror mapping of integration endpoints                
                // to mocked endpoints
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

                if (mockedEndpoint is TwoWayReceiveEndpoint)
                {
                    // Integration two way receive endpoint is mapped to a mock two way send point
                    SetupSendRequestAndReceiveResponse(mockedEndpoint as TwoWayReceiveEndpoint);
                }

                if (mockedEndpoint is TwoWaySendEndpoint)
                {
                    // Integration two way send endpoint is mapped to a mock two way receive point
                    SetupReceiveRequestAndSendResponse(mockedEndpoint as TwoWaySendEndpoint);
                }
            }

            return this;

        }

        private void SetupReceive(SendEndpoint sendEndpoint)
        {
            if (this.operationExpectations.ContainsKey(sendEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                return;
            }

            var receiveOperation = new MessageOperationExpectation()
            {
                SendEndpoint = sendEndpoint,
                MockMessageServer = new StreamingNamedPipeServer(
                    new Uri(sendEndpoint.URL).AbsolutePath)
            };

            // We start the server as this is the whole point of the setup process
            // The mock should be ready to receive messages from the integration
            // Before calling Start we need to hook the event handler for ReadCompleted
            // This brings some challanges with the chosen model, as the mock should then 
            // expose a public property exhibiting the queue that contains the received messages
            receiveOperation.MockMessageServer.ReadCompleted += MockMessageServer_ReadCompleted;
            receiveOperation.MockMessageServer.Start();


            // TODO: There should be added a check for uniqueness of the key
            operationExpectations.Add(sendEndpoint.URL, receiveOperation);

        }

        private void SetupSend(ReceiveEndpoint receiveEndpoint)
        {
            if (this.operationExpectations.ContainsKey(receiveEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                return;
            }

            var sendOperation = new MessageOperationExpectation()
            {
                ReceiveEndpoint = receiveEndpoint,
                MockMessageClient = new StreamingNamedPipeClient(new System.Uri(receiveEndpoint.URL))
            };

            operationExpectations.Add(receiveEndpoint.URL, sendOperation);

            return;

        }

        private void SetupReceiveRequestAndSendResponse(TwoWaySendEndpoint sendReceiveEndpoint)
        {
            var receiveSendOperation = new MessageOperationExpectation()
            {
                TwoWaySendEndpoint = sendReceiveEndpoint,
                MockMessageServer = new StreamingNamedPipeServer(
                    new Uri(sendReceiveEndpoint.URL).AbsolutePath)
            };

            receiveSendOperation.MockMessageServer.ReadCompleted += MockMessageServer_ReadCompleted;
            receiveSendOperation.MockMessageServer.Start();

            operationExpectations.Add(sendReceiveEndpoint.URL, receiveSendOperation);

        }

        private void SetupSendRequestAndReceiveResponse(TwoWayReceiveEndpoint receiveSendEndpoint)
        {
            var sendReceiveOperation = new MessageOperationExpectation()
            {
                TwoWayReceiveEndpoint = receiveSendEndpoint,
                MockMessageClient = new StreamingNamedPipeClient(new System.Uri(receiveSendEndpoint.URL))
            };

            operationExpectations.Add(receiveSendEndpoint.URL, sendReceiveOperation);

        }

        internal void TearDown()
        {
            // Stopping all the inbound message mock servers
            foreach (var item in this.operationExpectations.Values)
            {
                if (item.MockMessageServer != null)
                {
                    item.MockMessageServer.Stop();
                }
            }
        }
        
        private void MockMessageServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            lock (this.syncRoot)
            {
                this.receivedMessagesQueue.Enqueue(e);

                System.Diagnostics.Debug.WriteLine("Message received in the MockMessageServer_ReadCompleted handler");

                System.Threading.Monitor.Pulse(this.syncRoot);
            }
        }

        public Mold<TAddresses> Receive(
            Func<TestContext, TAddresses, SendEndpoint> sender,
            Func<int, System.IO.Stream, bool> validator)
        {
            return ReceiveImplementation(sender, validator);
        }

        public Mold<TAddresses> Receive(
            Expression<Func<TAddresses, string>> sendAddress,
            Action<SendEndpoint> configurator,
            Action<TestContext> contextAction,
            Func<int, System.IO.Stream, bool> validator)
        {

            return ReceiveImplementation((c, a) =>
            {
                string url = sendAddress.Compile()(a);

                var endpoindConfig = new SendEndpoint()
                {
                    URL = url
                };

                configurator(endpoindConfig);

                // TODO: Check whether the expected values were configured
                contextAction(c);

                return endpoindConfig;
            },
            validator);
        }

        public Mold<TAddresses> Receive(
            Expression<Func<TAddresses, string>> sendAddress,
            int timeoutInSeconds,
            int expectedMessageCount,
            System.Text.Encoding messageEncoding,
            Action<TestContext> contextAction,
            Func<int, System.IO.Stream, bool> validator)
        {

            return ReceiveImplementation((c, a) =>
            {
                string url = sendAddress.Compile()(a);

                var endpoindConfig = new SendEndpoint()
                {
                    URL = url,
                    TimeoutInSeconds = timeoutInSeconds,
                    MessageEncoding = messageEncoding,
                    ExpectedMessageCount = expectedMessageCount
                };

                // TODO: Check whether the expected values were configured
                contextAction(c);

                return endpoindConfig;
            },
            validator);
        }

        public Mold<TAddresses> Send(Func<TestContext, TAddresses, ReceiveEndpoint> receiver)
        {
            return this.SendImplementation(receiver);
        }

        public Mold<TAddresses> Send(
            Expression<Func<TAddresses, string>> receivingAddress,
            Action<ReceiveEndpoint> endpointConfig,
            Action<TestContext> contextAction)
        {
            return this.SendImplementation((c, a) =>
            {
                string url = receivingAddress.Compile()(a);

                var receiveEndpoint = new ReceiveEndpoint()
                {
                    URL = url
                };

                endpointConfig(receiveEndpoint);

                contextAction(c);

                return receiveEndpoint;
            });
        }

        public Mold<TAddresses> Send(
           Expression<Func<TAddresses, string>> receivingAddress,
           Func<ReceiveEndpoint, string> requestFile,
           Func<ReceiveEndpoint, System.Text.Encoding> fileEncoding,
           Func<ReceiveEndpoint, int> timeoutInSeconds,
           Action<TestContext> contextAction)
        {
            return this.SendImplementation((c, a) =>
            {
                string url = receivingAddress.Compile()(a);
                
                //TODO: Lookup in the dictionary of endpoints for the corresponding receive one
                var receiveEndpoint = new ReceiveEndpoint()
                {
                    URL = url
                };

                requestFile(receiveEndpoint);
                fileEncoding(receiveEndpoint);
                timeoutInSeconds(receiveEndpoint);

                contextAction(c);

                return receiveEndpoint;
            });
        }

        public Mold<TAddresses> Send(
           Expression<Func<TAddresses, string>> receivingAddress,
           string requestFile,
           System.Text.Encoding fileEncoding,
           int timeoutInSeconds,
           Action<TestContext> contextAction)
        {
            return this.SendImplementation((c, a) =>
            {
                string url = receivingAddress.Compile()(a);

                //TODO: Lookup in the dictionary of endpoints for the corresponding receive one
                var receiveEndpoint = new ReceiveEndpoint()
                {
                    URL = url,
                    RequestFilePath = requestFile,
                    MessageEncoding = fileEncoding,
                    TimeoutInSeconds = timeoutInSeconds
                };

                contextAction(c);

                return receiveEndpoint;
            });
        }

        public Mold<TAddresses> ReceiveRequestAndSendResponse(
            Func<TestContext, TAddresses, SendEndpoint> sender,
            Func<int, System.IO.Stream, bool> validator,
            Func<System.IO.Stream, ResponseStrategy> responseSelector)
        {
            return ReceiveImplementation(
                sender,
                validator,
                responseSelector,
                SendResponse);
        }

        public Mold<TAddresses> SendRequestAndReceiveResponse(
            Func<TestContext, TAddresses, ReceiveEndpoint> receiver,
            Func<System.IO.Stream, bool> validator)
        {
            return this.SendImplementation(
                receiver,
                validator,
                this.ReceiveResponse);
        }

        public Mold<TAddresses> InParallel(params Func<Mold<TAddresses>, Mold<TAddresses>>[] parallelActions)
        {
            //var actionsList = parallelActions(new List<TestMold<TAddresses>>());

            foreach (var action in parallelActions)
            {
                var task = new Task<Mold<TAddresses>>(
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
        /// 
        /// </summary>
        public void CleanUp()
        {

            // Cleanup any parallel tasks configured
            foreach (var operation in parallelOperationsList)
            {
                if (!operation.IsCompleted)
                {
                    operation.Wait();
                }
            }

            // Clear down the casting mock
            this.TearDown();

            // Clearing the list
            parallelOperationsList.Clear();
        }

        /// <summary>
        /// This is the implementation method of the Receive operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="validator"></param>
        /// <param name="responseSender"></param>
        /// <returns></returns>
        private Mold<TAddresses> ReceiveImplementation(
            Func<TestContext, TAddresses, SendEndpoint> sender,
            Func<int, System.IO.Stream, bool> validator,
            Func<System.IO.Stream, ResponseStrategy> responseSelector = null,
            Action<MessageOperationExpectation, ResponseStrategy, int> responseSender = null)
        {
            var sendEndpoint = sender(this.testContext, this.casting.mockAddresses);

            var endpointSetup = this.operationExpectations
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
                    validator(i, receivedMessage.MessageStream);

                    if (responseSender != null)
                    {
                        if (responseSelector == null)
                        {
                            throw new InvalidOperationException("No response selector defined");
                        }

                        var responseStrategy = responseSelector(receivedMessage.MessageStream);
                        // In case a response delegate is defined we call it too
                        responseSender(endpointSetup,
                            responseStrategy,
                            receivedMessage.ConnectionId);
                    }
                }

            }

            return this;
        }

        private Mold<TAddresses> SendImplementation(
            Func<TestContext, TAddresses, ReceiveEndpoint> receiver,
            Func<System.IO.Stream, bool> validator = null,
            Func<MessageOperationExpectation, System.IO.Stream> responseReceiver = null)
        {
            // We fetch first the actual receiver endpoint
            var receiverEndpoint = receiver(this.testContext, this.casting.mockAddresses);

            var endpointSetup = this.operationExpectations
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

                // TODO: Figure out a clever way to represent the paradigm of a receiver endpoint
                // from the perception of the TestMold. This is because the receiver endpoint needs
                // to include the path to the file containing the request that is to be sent to the
                // endpoint represented by the instance of this object.
                using (var sr =
                    new System.IO.StreamReader(
                        receiverEndpoint.RequestFilePath,
                        receiverEndpoint.MessageEncoding))
                {
                    endpointSetup.MockMessageClient
                        .WriteStream(sr.BaseStream);
                }

                if (responseReceiver != null)
                {
                    var responseMessage = responseReceiver(endpointSetup);

                    // Finally we validate the received response message
                    if (validator != null)
                    {
                        validator(responseMessage);
                    }
                }
            }
            finally
            {
                endpointSetup.MockMessageClient
                    .Disconnect();
            }

            return this;
        }

        private void SendResponse(MessageOperationExpectation endpointSetup, ResponseStrategy responseStrategy, int connectionId)
        {
            // Fetch the message based on the configured strategy
            var responseMessage = responseStrategy.FetchResponseMessage();

            endpointSetup.MockMessageServer
                .WriteStream(
                    connectionId,
                    responseMessage);

        }

        /// <summary>
        /// Receive response implementation
        /// TODO: create a higher abstraction representation of a message for easier work in the validation methods
        /// </summary>
        /// <returns></returns>
        private System.IO.Stream ReceiveResponse(MessageOperationExpectation endpointSetup)
        {
            // We receive the response
            var responseStream = endpointSetup.MockMessageClient
                .ReadStream();

            return responseStream;
        }

        //public void Fill()
        //{
        //    if (this.casting == null)
        //    {
        //        throw new InvalidOperationException("No casting povided!");
        //    }

        //    foreach (var recordedExpectation in casting.recordedExpectations)
        //    {

        //    }
        //}
    }
}
