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
    public class IntegrationMock<TAddresses> where TAddresses : class
    {
        internal TAddresses mockAddresses;        

        //internal object syncRoot = new object();

        internal Dictionary<string, MockEndpoint> endpointsMap;

        public IntegrationMock()
        {
            endpointsMap = new Dictionary<string, MockEndpoint>(3);
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
        public IntegrationMock<TAddresses> SetupReceive(Expression<Func<TAddresses, string>> receiver)
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

            endpointsMap.Add(receiveEndpoint.URL, receiveEndpoint);            

            return this;
        }        

        public IntegrationMock<TAddresses> SetupSend(Expression<Func<TAddresses, string>> sender)
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
            
            endpointsMap.Add(sendEndpoint.URL, sendEndpoint);

            return this;
            
        }

        public IntegrationMock<TAddresses> SetupReceiveRequestAndSendResponse(Expression<Func<TAddresses, string>> receiver)
        {
            var receiveSendEndpoint = new TwoWayReceiveEndpoint();

            // Invoke the callback for setting the send endpoint properties as well as the expectation method
            receiveSendEndpoint.URL = receiver.Compile()(this.mockAddresses);

            endpointsMap.Add(receiveSendEndpoint.URL, receiveSendEndpoint);

            return this;
        }

        public IntegrationMock<TAddresses> SetupSendRequestAndReceiveResponse(Expression<Func<TAddresses, string>> sender)
        {
            var sendReceiveEndpoint = new TwoWaySendEndpoint();

            // Invoke the callback for setting the send endpoint properties as well as the expectation method
            sendReceiveEndpoint.URL = sender.Compile()(this.mockAddresses);            

            endpointsMap.Add(sendReceiveEndpoint.URL, sendReceiveEndpoint);

            return this;
        }

        public Mold<TAddresses> CreateMold()
        {
            return ConcreteMold<TAddresses>.CreateMold(this);
        }

        // Hiding the implementation of the abstract Mold class
        internal class ConcreteMold<TAddresses2> : Mold<TAddresses2> where TAddresses2 : class
        {
            protected ConcreteMold(IntegrationMock<TAddresses2> casting) : base(casting)
            {

            }
            internal static Mold<TAddresses2> CreateMold(IntegrationMock<TAddresses2> casting)
            {
                return new ConcreteMold<TAddresses2>(casting)
                    .WireUp();
            }
        }
    }

    public class SingleMessageExpectation
    {
    }
}
