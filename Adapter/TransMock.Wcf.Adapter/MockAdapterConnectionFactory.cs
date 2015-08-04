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

/// -----------------------------------------------------------------------------------------------------------
/// Module      :  MockAdapterConnectionFactory.cs
/// Description :  Defines the connection factory for the target system.
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.ServiceModel.Description;
using System.Text;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// The mock adapter connection factory class
    /// </summary>
    public class MockAdapterConnectionFactory : IConnectionFactory
    {
        #region Private Fields

        /// <summary>
        /// Stores the client credentials
        /// </summary> 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields",
            Justification = "Needed as per design")]
        private ClientCredentials clientCredentials;

        /// <summary>
        /// Stores the adapter class
        /// </summary> 
        private MockAdapter adapter;
        
        /// <summary>
        /// The connection URI
        /// </summary>
        private MockAdapterConnectionUri connectionUri;

        #endregion Private Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterConnectionFactory"/> class
        /// </summary>
        /// <param name="connectionUri">The connection Uri</param>        
        /// <param name="clientCredentials">THe client credentials for the adapter connection</param>
        /// <param name="adapter">The adapter instance</param>
        public MockAdapterConnectionFactory(
            ConnectionUri connectionUri,
            ClientCredentials clientCredentials,
            MockAdapter adapter)
        {
            this.clientCredentials = clientCredentials;
            this.adapter = adapter;
            this.connectionUri = connectionUri as MockAdapterConnectionUri;
        }

        #region Public Properties

        /// <summary>
        /// Gets the adapter
        /// </summary>
        public MockAdapter Adapter
        {
            get
            {
                return this.adapter;
            }
        }

        /// <summary>
        /// Gets the adapter connection Uri
        /// </summary>
        public MockAdapterConnectionUri ConnectionUri 
        {
            get
            {
                return this.connectionUri;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Creates the connection to the target system
        /// </summary>
        /// <returns>An instance of the adapter connection</returns>
        public IConnection CreateConnection()
        {
            return new MockAdapterConnection(this);
        }

        #endregion Public Methods
    }
}
