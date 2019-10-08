using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransMock.Mockifier.Parser
{
    internal struct EndpointSettings
    {
        /// <summary>
        /// The endpoint URL
        /// </summary>
        public string Address;

        /// <summary>
        /// Flag indicating whether it is a two way end point
        /// </summary>
        public bool IsTwoWay;

        /// <summary>
        /// The endpoint direction
        /// </summary>
        public EndpointDirection Direction;
         
    }

    internal enum EndpointDirection
    {
        Receive,
        Send
    }
}
