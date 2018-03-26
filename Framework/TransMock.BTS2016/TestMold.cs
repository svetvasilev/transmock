using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransMock
{
    /// <summary>
    /// This class represent the actual mocked mold where the operations are mirrored compared to the
    /// actual integration represented by the TestCasting object. 
    /// Send operation from the mold isntance corresponds to a receive operation in the casting (integration)
    /// Receive operation in the mold corresponds to a send operation from the casting, and so on.
    /// </summary>
    public class TestMold<TAddresses> where TAddresses : class
    {
        private TestCasting<TAddresses> casting;

        private TestContext testContext;

        private List<Task<TestMold<TAddresses>>> parallelOperationsList;
        public TestMold()
        {
            testContext = new TestContext();

            parallelOperationsList = new List<Task<TestMold<TAddresses>>>(3);
        }

        public TestMold(TestCasting<TAddresses> casting) : this()
        {
            this.casting = casting;
        }

        public TestMold<TAddresses> Receive(
            Func<TestContext, TAddresses, SendEndpoint> sender, 
            Func<int, System.IO.Stream, bool> validator)
        {
            return ReceiveImplementation(sender, validator);
        }        

        public TestMold<TAddresses> Send(Func<TestContext, TAddresses, ReceiveEndpoint> receiver)
        {
            return this.SendImplementation(receiver);
        }

        public TestMold<TAddresses> ReceiveRequestAndSendResponse(
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

        public TestMold<TAddresses> SendRequestAndReceiveResponse(
            Func<TestContext, TAddresses, ReceiveEndpoint> receiver,
            Func<System.IO.Stream, bool> validator)
        {
            return this.SendImplementation(
                receiver,
                validator,
                this.ReceiveResponse);   
        }

        public TestMold<TAddresses> InParallel(params Func<TestMold<TAddresses>, TestMold<TAddresses>>[] parallelActions)
        {
            //var actionsList = parallelActions(new List<TestMold<TAddresses>>());

            foreach (var action in parallelActions)
            {
                var task = new Task<TestMold<TAddresses>>(
                    () => {
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
            this.casting.TearDown();

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
        private TestMold<TAddresses> ReceiveImplementation(
            Func<TestContext, TAddresses, SendEndpoint> sender,
            Func<int,System.IO.Stream, bool> validator,
            Func<System.IO.Stream, ResponseStrategy> responseSelector = null,
            Action<MessageOperationExpectation, ResponseStrategy, int> responseSender = null)
        {
            var sendEndpoint = sender(this.testContext, this.casting.mockAddresses);

            var endpointSetup = this.casting.endpointsMap
                .Where(kvp => kvp.Key == sendEndpoint.URL)
                .FirstOrDefault().Value;

            if (endpointSetup == null)
            {
                throw new InvalidOperationException("No corresponding endpoint setup found");
            }

            lock (this.casting.syncRoot)
            {
                // Covering debatching scenarios
                for (int i = 0; i < sendEndpoint.ExpectedMessageCount; i++)
                {
                    // Now we wait for the reception of a message
                    bool waitElapsed = System.Threading.Monitor
                        .Wait(
                            this.casting.syncRoot,
                            sendEndpoint.TimeoutInSeconds * 1000);

                    if (!waitElapsed)
                    {
                        throw new TimeoutException("No message received for the wait time set.");
                    }

                    // Now we read the message from the message queue

                    var receivedMessage = this.casting.receivedMessagesQueue.Dequeue();

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

        private TestMold<TAddresses> SendImplementation(
            Func<TestContext, TAddresses, ReceiveEndpoint> receiver,
            Func<System.IO.Stream, bool> validator = null,
            Func<MessageOperationExpectation, System.IO.Stream> responseReceiver = null)
        {
            // We fetch first the actual receiver endpoint
            var receiverEndpoint = receiver(this.testContext, this.casting.mockAddresses);

            var endpointSetup = casting.endpointsMap
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

    public class ReceiveEndpoint : MockEndpoint
    {
        public string RequestFilePath { get; set; }
    }

    public class MockedAddressBase
    {

    }
}
