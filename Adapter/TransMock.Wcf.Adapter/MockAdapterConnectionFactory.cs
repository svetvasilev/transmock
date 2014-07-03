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
using System.Text;
using System.IdentityModel.Selectors;
using System.ServiceModel.Description;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    public class MockAdapterConnectionFactory : IConnectionFactory
    {
        #region Private Fields

        // Stores the client credentials
        private ClientCredentials clientCredentials;
        // Stores the adapter class
        private MockAdapter adapter;
        //The connection URI
        private MockAdapterConnectionUri connectionUri;

        #endregion Private Fields

        /// <summary>
        /// Initializes a new instance of the WCFMockAdapterConnectionFactory class
        /// </summary>
        public MockAdapterConnectionFactory(ConnectionUri connectionUri
            , ClientCredentials clientCredentials
            , MockAdapter adapter)
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

        public MockAdapterConnectionUri ConnectionUri {
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
        public IConnection CreateConnection()
        {
            return new MockAdapterConnection(this);
        }

        #endregion Public Methods
    }
}
