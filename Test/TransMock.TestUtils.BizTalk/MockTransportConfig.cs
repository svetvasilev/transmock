/***************************************
//   Copyright 2014 - Svetoslav Vasilev

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

using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>
        /// A constant representing the mock transport type name
        /// </summary>
        private const string TransportTypeName = "WCF-Custom";

        /// <summary>
        /// The address of the endpoint
        /// </summary>
        private string address;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockTransportConfig"/> class
        /// </summary>
        public MockTransportConfig()
        {        
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockTransportConfig"/> class with the given endpoint name
        /// </summary>
        /// <param name="endPointName">The endpoint name for the mock transport config</param>
        public MockTransportConfig(string endPointName)
        {
            this.address = string.Format(
                CultureInfo.InvariantCulture,
                "mock://localhost/Dynamic{0}", 
                endPointName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockTransportConfig"/> class with the given host and endpoint names
        /// </summary>
        /// <param name="hostName">The host name of the endpoint</param>
        /// <param name="endPointName">The name of the endpoint</param>
        public MockTransportConfig(string hostName, string endPointName)
        {
            this.address = string.Format(
                CultureInfo.InvariantCulture,
                "mock://{0}/Dynamic{1}", 
                hostName, 
                endPointName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockTransportConfig"/> class with the given host, endpoint and operation names names
        /// </summary>
        /// <param name="hostName">The host name of the endpoint</param>
        /// <param name="endPointName">The name of the endpoint</param>
        /// <param name="operationName">The name of the operation</param>
        public MockTransportConfig(string hostName, string endPointName, string operationName)
        {
            this.address = string.Format(
                CultureInfo.InvariantCulture,
                "mock://{0}/Dynamic{1}/{2}", 
                hostName, 
                endPointName, 
                operationName);
        }

        /// <summary>
        /// Gets the transport type being always WCF-custom
        /// </summary>
        public string TransportType
        {
            get
            {
                return TransportTypeName;
            }
        }

        /// <summary>
        /// Gets the address of the mock endpoint
        /// </summary>
        public string Address
        {
            get
            {
                return this.address;
            }
        }
    }
}
