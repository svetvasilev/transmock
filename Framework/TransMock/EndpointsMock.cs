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
    /// The casting mock represents a service/integration implementation from its endpoints point of view and it is setup once for a test suite.
    /// This is because a service/integration flow has a finite and predictable set of receive and send ports at any given time.
    /// What varies during execution is the messages received over/sent to a given endpoint instance.
    /// This behavior is driven by the Mold class where the corresponding message operations are set.
    /// This is why the Setup methods match the direction of the message flow, e.g. SetupReceive is setting up a receive
    /// endpoint and vice versa.
    /// </summary>
    public class EndpointsMock<TAddresses> where TAddresses : Addressing.EndpointAddress
    {
        internal TAddresses mockAddresses;        

        //internal object syncRoot = new object();

        internal Dictionary<string, MockedEndpoint> endpointsMap;

        /// <summary>
        /// Creates an instance of class <class ref="CastingMock" /> 
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
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public EndpointsMock<TAddresses> SetupReceiveRequestAndSendResponse(Expression<Func<TAddresses, Addressing.TwoWayReceiveAddress>> receiver)
        {
            var receiveSendEndpoint = new TwoWayReceiveEndpoint();

            // Invoke the callback for setting the send endpoint properties as well as the expectation method
            receiveSendEndpoint.URL = receiver.Compile()(this.mockAddresses).Value;

            endpointsMap.Add(receiveSendEndpoint.URL, receiveSendEndpoint);

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public EndpointsMock<TAddresses> SetupSendRequestAndReceiveResponse(Expression<Func<TAddresses, Addressing.TwoWaySendAddress>> sender)
        {
            var sendReceiveEndpoint = new TwoWaySendEndpoint();

            // Invoke the callback for setting the send endpoint properties as well as the expectation method
            sendReceiveEndpoint.URL = sender.Compile()(this.mockAddresses).Value;            

            endpointsMap.Add(sendReceiveEndpoint.URL, sendReceiveEndpoint);

            return this;
        }

        /// <summary>
        /// Creates a Mold instance that is modeled as per the casting instance
        /// </summary>
        /// <returns></returns>
        public InverseMessagingClient<TAddresses> CreateMessagingPatternEmulator()
        {
            return ConcreteMessagingPatternEmulator<TAddresses>.CreateInstance(this);
        }

        // Hiding the implementation of the abstract Mold class
        internal class ConcreteMessagingPatternEmulator<TAddresses2> : InverseMessagingClient<TAddresses2> where TAddresses2 : Addressing.EndpointAddress
        {
            protected ConcreteMessagingPatternEmulator(EndpointsMock<TAddresses2> casting) : base(casting)
            {

            }
            internal static InverseMessagingClient<TAddresses2> CreateInstance(EndpointsMock<TAddresses2> casting)
            {
                return new ConcreteMessagingPatternEmulator<TAddresses2>(casting)
                    .WireUp();
            }
        }
    }
    
}
