
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
/// Module      :  EndpointsMock.cs
/// Description :  This class implements the logic for mocking service endpoints in order to be tested.
/// -----------------------------------------------------------------------------------------------------------

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
    /// This class represents a mock of a service/integration implementation from its endpoints point of view and it is set up once for a test suite.
    /// This is because a service/integration flow has a finite and predictable set of receive and send endpoints.
    /// What varies during execution is the messages received over/sent to a given endpoint instance.
    /// This behavior is driven by the <see cref="TestMessagingClient{TAddresses}"/> class from where the corresponding messaging operations are performed.
    /// This is why the Setup methods match the direction of the message flow, e.g. SetupReceive is setting up a receive
    /// endpoint and vice versa.
    /// </summary>
    public class EndpointsMock<TAddresses> where TAddresses : Addressing.EndpointAddress
    {
        internal TAddresses mockAddresses;        

        //internal object syncRoot = new object();

        internal Dictionary<string, MockedEndpoint> endpointsMap;

        /// <summary>
        /// Creates an instance of the <see cref="EndpointsMock{TAddresses}"/> class
        /// </summary>
        public EndpointsMock()
        {
            endpointsMap = new Dictionary<string, MockedEndpoint>(3);
            // Create a single instance of the mock addresses class to be used to fetch
            // the adress URLs from it
            mockAddresses = Activator.CreateInstance(typeof(TAddresses)) as TAddresses;            
        }

        /// <summary>
        /// The func in the expression is hardcoded to return strings, as the Mock addresses class has only 
        /// getters of type string
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns>The current instance of the <see cref="EndpointsMock{TAddresses}"/> class</returns>
        public EndpointsMock<TAddresses> SetupReceive(Expression<Func<TAddresses, Addressing.OneWayReceiveAddress>> receiver)
        {   
            var receiveEndpoint = new ReceiveEndpoint();

            // Compile the expression and fetch the value of the corresponding property
            
            receiveEndpoint.URL = receiver.Compile()(this.mockAddresses).Value;

            if (this.endpointsMap.ContainsKey(receiveEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                return this;
            }

            endpointsMap.Add(receiveEndpoint.URL, receiveEndpoint);            

            return this;
        }

        /// <summary>
        /// Sets up a mock for a send endpoint
        /// </summary>
        /// <param name="sender">An expression that returns the address of the send endpoint</param>
        /// <returns>The current instance of the <see cref="EndpointsMock{TAddresses}"/> class</returns>
        public EndpointsMock<TAddresses> SetupSend(Expression<Func<TAddresses, Addressing.OneWaySendAddress>> sender)
        {
            var sendEndpoint = new SendEndpoint();

            // Invoke the callback for setting the send endpoint properties as well as the expectation method
            sendEndpoint.URL = sender.Compile()(this.mockAddresses).Value;

            if (this.endpointsMap.ContainsKey(sendEndpoint.URL))
            {
                // We have an expectation set for this endpoint
                // so we exit gracefully
                return this;
            }
            
            endpointsMap.Add(sendEndpoint.URL, sendEndpoint);

            return this;
            
        }

        /// <summary>
        /// Sets up a mock for a 2-way receive operation where a request is received and a response is supplied back.
        /// </summary>
        /// <param name="receiver">An expression that returns the address of the 2-way receive endpoint</param>
        /// <returns>The current instance of the <see cref="EndpointsMock{TAddresses}"/> class</returns>
        public EndpointsMock<TAddresses> SetupReceiveRequestAndSendResponse(Expression<Func<TAddresses, Addressing.TwoWayReceiveAddress>> receiver)
        {
            var receiveSendEndpoint = new TwoWayReceiveEndpoint();

            // Invoke the callback for setting the send endpoint properties as well as the expectation method
            receiveSendEndpoint.URL = receiver.Compile()(this.mockAddresses).Value;

            endpointsMap.Add(receiveSendEndpoint.URL, receiveSendEndpoint);

            return this;
        }

        /// <summary>
        /// Sets up an endpoint that follows 2-way outbound communication pattern - 
        /// send a request and expects a response synchronously
        /// </summary>
        /// <param name="sender">An expression that returns the address of the send endpoint for this operation</param>
        /// <returns>The current instance of the <see cref="EndpointsMock{TAddresses}"/> class</returns>
        public EndpointsMock<TAddresses> SetupSendRequestAndReceiveResponse(Expression<Func<TAddresses, Addressing.TwoWaySendAddress>> sender)
        {
            var sendReceiveEndpoint = new TwoWaySendEndpoint();

            // Invoke the callback for setting the send endpoint properties as well as the expectation method
            sendReceiveEndpoint.URL = sender.Compile()(this.mockAddresses).Value;            

            endpointsMap.Add(sendReceiveEndpoint.URL, sendReceiveEndpoint);

            return this;
        }

        /// <summary>
        /// Creates a <see cref="TestMessagingClient{TAddresses}"/> instance that can be used to send messages for testing 
        /// a service that is represented by this endpoints mock instance
        /// </summary>
        /// <returns>An instance of the <see cref="TestMessagingClient{TAddresses}"/> class</returns>
        public TestMessagingClient<TAddresses> CreateMessagingClient()
        {
            return ConcreteTestMessagingClient<TAddresses>.CreateInstance(this);
        }

        // Hiding the implementation of the abstract TestMessagingClient class
        internal class ConcreteTestMessagingClient<TAddresses2> : TestMessagingClient<TAddresses2> where TAddresses2 : Addressing.EndpointAddress
        {
            protected ConcreteTestMessagingClient(EndpointsMock<TAddresses2> mock) : base(mock)
            {

            }

            internal static TestMessagingClient<TAddresses2> CreateInstance(EndpointsMock<TAddresses2> mock)
            {
                return new ConcreteTestMessagingClient<TAddresses2>(mock)
                    .WireUp();
            }
        }
    }
    
}
