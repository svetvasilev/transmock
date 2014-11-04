using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransMock.TestUtils.BizTalk
{
    /// <summary>
    /// Contains the basic configuration settings for a dynamic send port using the mock adapter
    /// </summary>
    [Serializable]
    public class MockTransportConfig
    {
        private const string transportType = "WCF-Custom";

        private string address;

        public MockTransportConfig()
        {
        
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPointName"></param>
        public MockTransportConfig(string endPointName)
        {
            address = string.Format("mock://localhost/{0}", endPointName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="endPointName"></param>
        public MockTransportConfig(string hostName, string endPointName)
        {
            address = string.Format("mock://{0}/{1}", hostName, endPointName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="endPointName"></param>
        /// <param name="operationName"></param>
        public MockTransportConfig(string hostName, string endPointName, string operationName)
        {
            address = string.Format("mock://{0}/{1}/{2}", hostName, endPointName, operationName);
        }

        /// <summary>
        /// Gets the transport type being always WCF-custom
        /// </summary>
        public string TransportType
        {
            get
            {
                return transportType;
            }
        }

        /// <summary>
        /// Gets the address of the mock endpoint
        /// </summary>
        public string Address
        {
            get
            {
                return address;
            }
        }
    }
}
