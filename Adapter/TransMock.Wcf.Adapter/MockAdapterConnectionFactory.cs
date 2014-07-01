/// -----------------------------------------------------------------------------------------------------------
/// Module      :  WCFMockAdapterConnectionFactory.cs
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
