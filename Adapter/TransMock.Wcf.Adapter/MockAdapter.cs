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
/// Module      :  MockAdapter.cs
/// Description :  The main adapter class which inherits from Adapter
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.ServiceModel.Description;
using System.Text;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// The mock adapter class
    /// </summary>
    public class MockAdapter : Microsoft.ServiceModel.Channels.Common.Adapter
    {
        /// <summary>
        /// Scheme associated with the adapter
        /// </summary> 
        internal const string SCHEME = "mock";

        /// <summary>
        /// Namespace for the proxy that will be generated from the adapter schema
        /// </summary> 
        internal const string SERVICENAMESPACE = "http://www.transmock.com/Wcf/Adapter";

        /// <summary>
        /// Initializes the AdapterEnvironmentSettings class
        /// </summary> 
        private static AdapterEnvironmentSettings environmentSettings = new AdapterEnvironmentSettings();

        #region Custom Generated Fields
        /// <summary>
        /// The encoding used by the adapter
        /// </summary>
        private string encoding;

        /// <summary>
        /// A list of separated by a delimiter adapter properties for promotion
        /// </summary>
        private string promotedProperties;

        #endregion Custom Generated Fields

        #region  Constructor
        /// <summary>
        /// Initializes a new instance of the MockAdapter class
        /// </summary>
        public MockAdapter()
            : base(environmentSettings)
        {
            Settings.Metadata.DefaultMetadataNamespace = SERVICENAMESPACE;
        }

        /// <summary>
        /// Initializes a new instance of the MockAdapter class with a binding
        /// </summary>
        /// <param name="binding">The binding instance from which the adapter will be initialized</param>
        public MockAdapter(MockAdapter binding)
            : base(binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException("binding");                
            }

            this.Encoding = binding.Encoding;
            this.PromotedProperties = binding.PromotedProperties;
        }

        #endregion Constructor

        #region Custom Generated Properties

        /// <summary>
        /// Gets or sets the encoding to be used for the messages
        /// </summary>
        [System.Configuration.ConfigurationProperty("Encoding")]
        public string Encoding
        {            
            get
            {
                return this.encoding;
            }

            set
            {
                this.encoding = value;
            }
        }

        /// <summary>
        /// Gets or sets the list of promoted properties of the actual adapter
        /// </summary>
        [System.Configuration.ConfigurationProperty("PromotedProperties")]
        public string PromotedProperties
        {
            get
            {
                return this.promotedProperties;
            }

            set
            {
                this.promotedProperties = value;
            }
        }

        #endregion Custom Generated Properties

        #region Public Properties

        /// <summary>
        /// Gets the URI transport scheme that is used by the adapter
        /// </summary>
        public override string Scheme
        {
            get
            {
                return SCHEME;
            }
        }

        #endregion Public Properties

        #region Protected Methods
        /// <summary>
        /// Gets the namespace that is used when generating schema and WSDL
        /// </summary>
        protected override string Namespace
        {
            get
            {
                return SERVICENAMESPACE;
            }
        }

        /// <summary>
        /// Creates a ConnectionUri instance from the provided Uri
        /// </summary>
        /// <param name="uri">The connection Uri</param>
        /// <returns>An instance of the connection Uri</returns>
        protected override ConnectionUri BuildConnectionUri(Uri uri)
        {
            return new MockAdapterConnectionUri(uri);
        }

        /// <summary>
        /// Builds a connection factory from the ConnectionUri and ClientCredentials
        /// </summary>
        /// <param name="connectionUri">The connection Uri</param>
        /// <param name="clientCredentials">The client credentials for the connection</param>
        /// <param name="context">The binding context</param>
        /// <returns>An instance of the connection factory</returns>
        protected override IConnectionFactory BuildConnectionFactory(
            ConnectionUri connectionUri,
            ClientCredentials clientCredentials,
            System.ServiceModel.Channels.BindingContext context)
        {
            return new MockAdapterConnectionFactory(connectionUri, clientCredentials, this);
        }

        /// <summary>
        /// Returns a clone of the adapter object
        /// </summary>
        /// <returns>An instance of the mock adapter</returns>
        protected override Microsoft.ServiceModel.Channels.Common.Adapter CloneAdapter()
        {
            return new MockAdapter(this);
        }

        /// <summary>
        /// Indicates whether the provided TConnectionHandler is supported by the adapter or not
        /// </summary>
        /// <typeparam name="TConnectionHandler">The connection handler type</typeparam>
        /// <returns>A boolean value indicating whether the handler type provided as a template parameter is supported</returns>
        protected override bool IsHandlerSupported<TConnectionHandler>()
        {
            return                  
                typeof(IOutboundHandler) == typeof(TConnectionHandler)                
                || typeof(IInboundHandler) == typeof(TConnectionHandler);
        }        

        #endregion Protected Methods
    }
}
