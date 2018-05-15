using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using TransMock.Communication.NamedPipes;

namespace TransMock
{
    /// <summary>
    /// The casting represents the integration implementation and it is setup once for a test suite.
    /// This is because the integration has a finite and predictable set of receive and send ports at any given time.
    /// What varies during execution is the number of messages received over/sent to a given port instance.
    /// This behavior is driven by the TestMold class where the corresponding expectations are set.
    /// This is why the Setup methods match the direction of the message flow, e.g. SetupReceive is setting up a receive
    /// endpoint and vice versa.
    /// IDEA: To use the casting for performing the mocking on the fly?
    /// </summary>
    public class TestCasting<TAddresses> where TAddresses : class
    {
        internal TAddresses mockAddresses;        

        internal object syncRoot = new object();

        internal Dictionary<string, MessageOperationExpectation> endpointsMap;

        internal Queue<AsyncReadEventArgs> receivedMessagesQueue;
        public TestCasting()
        {
            endpointsMap = new Dictionary<string, MessageOperationExpectation>(3);
            // Create a single instance of the mock addresses class to be used to fetch
            // the adress URLs from it
            mockAddresses = Activator.CreateInstance(typeof(TAddresses)) as TAddresses;

            receivedMessagesQueue = new Queue<AsyncReadEventArgs>(3);
        }

        /// <summary>
        /// The func in the expression is hardcoded to return strings, as the Mock addresses class has only 
        /// getters of type string
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public TestCasting<TAddresses> SetupSend(Expression<Func<TAddresses, string>> receiver)
        {   
            var receiveEndpoint = new ReceiveEndpoint();

            // Compile the expression and fetch the value of the corresponding property
            
            receiveEndpoint.URL = receiver.Compile()(this.mockAddresses);

            if (this.endpointsMap.ContainsKey(receiveEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                return this;
            }

            var receiveOperation = new MessageOperationExpectation()
            {
                ReceiveEndpoint = receiveEndpoint,
                MockMessageServer = new StreamingNamedPipeServer(
                    new Uri(receiveEndpoint.URL).AbsolutePath)
            };

            // We start the server as this is the whole point of the setup process
            // The mock should be ready to receive messages from the integration
            // Before calling Start we need to hook the event handler for ReadCompleted
            // This brings some challanges with the chosen model, as the mock should then 
            // expose a public property exhibiting the queue that contains the received messages
            receiveOperation.MockMessageServer.ReadCompleted += MockMessageServer_ReadCompleted;
            receiveOperation.MockMessageServer.Start();


            // TODO: There should be added a check for uniqueness of the key
            endpointsMap.Add(receiveEndpoint.URL, receiveOperation);

            return this;
        }        

        public TestCasting<TAddresses> SetupReceive(Expression<Func<TAddresses, string>> sender)
        {
            var sendEndpoint = new SendEndpoint();

            // Invoke the callback for setting the send endpoint properties as well as the expectation method
            sendEndpoint.URL = sender.Compile()(this.mockAddresses);

            if (this.endpointsMap.ContainsKey(sendEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                return this;
            }

            var sendOperation = new MessageOperationExpectation()
            {
                SendEndpoint = sendEndpoint,
                MockMessageClient = new StreamingNamedPipeClient(new System.Uri(sendEndpoint.URL))
            };

            endpointsMap.Add(sendEndpoint.URL, sendOperation);

            return this;
            
        }

        public TestCasting<TAddresses> SetupSendRequestAndReceiveResponse(Expression<Func<TAddresses, string>> receiver)
        {
            var receiveSendEndpoint = new TwoWayReceiveEndpoint();

            // Invoke the callback for setting the send endpoint properties as well as the expectation method
            receiveSendEndpoint.URL = receiver.Compile()(this.mockAddresses);

            var receiveSendOperation = new MessageOperationExpectation()
            {
                TwoWayReceiveEndpoint = receiveSendEndpoint,
                MockMessageServer = new StreamingNamedPipeServer(
                    new Uri(receiveSendEndpoint.URL).AbsolutePath)
            };

            receiveSendOperation.MockMessageServer.ReadCompleted += MockMessageServer_ReadCompleted;
            receiveSendOperation.MockMessageServer.Start();            

            endpointsMap.Add(receiveSendEndpoint.URL, receiveSendOperation);

            return this;
        }

        public TestCasting<TAddresses> SetupReceiveRequestAndSendResponse(Expression<Func<TAddresses, string>> sender)
        {
            var sendReceiveEndpoint = new TwoWaySendEndpoint();

            // Invoke the callback for setting the send endpoint properties as well as the expectation method
            sendReceiveEndpoint.URL = sender.Compile()(this.mockAddresses);

            var sendReceiveOperation = new MessageOperationExpectation()
            {
                TwoWaySendEndpoint = sendReceiveEndpoint,
                MockMessageClient = new StreamingNamedPipeClient(new System.Uri(sendReceiveEndpoint.URL))
            };

            endpointsMap.Add(sendReceiveEndpoint.URL, sendReceiveOperation);

            return this;
        }

        internal void TearDown()
        {
            // Stopping all the inbound message mock servers
            foreach (var item in this.endpointsMap.Values)
            {
                if (item.MockMessageServer != null)
                {
                    item.MockMessageServer.Stop();
                }
            }
        }

        //public TestCasting InParallel()
        //{

        //}

        private void MockMessageServer_ReadCompleted(object sender, AsyncReadEventArgs e)
        {
            lock (this.syncRoot)
            {
                this.receivedMessagesQueue.Enqueue(e);

                System.Threading.Monitor.Pulse(this.syncRoot);
            }
        }
    }
}
