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
/// Module      :  MockAdapterBinding.cs
/// Description :  This is the class used while creating a binding for an adapter
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// The mock adapter binding class
    /// </summary>
    public class MockAdapterBinding : AdapterBinding
    {        
        /// <summary>
        /// The binding scheme
        /// </summary>
        private const string BindingScheme = "mock";        

        #region Private Fields
        /// <summary>
        /// The mock adapter binding
        /// </summary>
        private MockAdapter binding;

        #endregion Private Fields

        #region Custom Generated Fields        
        /// <summary>
        /// The host name
        /// </summary>
        private string host;

        /// <summary>
        /// The system endpoint name
        /// </summary>
        private string systemEndpoint;

        /// <summary>
        /// The operation name
        /// </summary>
        private string operation;

        /// <summary>
        /// The encoding name used for message serialization
        /// </summary>
        private string encoding;

        /// <summary>
        /// The list of adapter properties for promotion
        /// </summary>
        private string promotedProperties;

        #endregion Custom Generated Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterBinding"/> class
        /// </summary>
        public MockAdapterBinding()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterBinding"/> class with a configuration name
        /// </summary>
        /// <param name="configName">The configuration name to be used to configure the adapter binding instance with</param>
        public MockAdapterBinding(string configName)
        {
            this.ApplyConfiguration(configName);
        }        

        #region Public Properties

        /// <summary>
        /// Gets the URI transport scheme that is used by the channel and listener factories that are built by the bindings.
        /// </summary>
        public override string Scheme
        {
            get
            {
                return BindingScheme;
            }
        }

        /// <summary>
        /// Returns a value indicating whether this binding supports metadata browsing.
        /// </summary>
        public override bool SupportsMetadataBrowse
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a value indicating whether this binding supports metadata retrieval.
        /// </summary>
        public override bool SupportsMetadataGet
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a value indicating whether this binding supports metadata searching.
        /// </summary>
        public override bool SupportsMetadataSearch
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the custom type of the ConnectionUri.
        /// </summary>
        public override Type ConnectionUriType
        {
            get
            {
                return typeof(MockAdapterConnectionUri);
            }
        }

        #endregion Public Properties

        #region Custom Generated Properties
        /// <summary>
        /// Gets or sets the host name for the connection
        /// </summary>
        [System.Configuration.ConfigurationProperty("Host", DefaultValue = "localhost")]
        public string Host
        {
            get
            {
                return this.host;
            }

            set
            {
                this.host = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the system endpoint
        /// </summary>
        [System.Configuration.ConfigurationProperty("SystemEndpoint", DefaultValue = "")]
        public string SystemEndpoint
        {
            get
            {
                return this.systemEndpoint;
            }

            set
            {
                this.systemEndpoint = value;
            }
        }

        /// <summary>
        /// Gets or sets the operation name for a given system endpoint
        /// </summary>
        [System.Configuration.ConfigurationProperty("Operation", DefaultValue = "")]
        public string Operation
        {
            get
            {
                return this.operation;
            }

            set
            {
                this.operation = value;
            }
        }

        /// <summary>
        /// Gets or sets the encoding to be used for message serialization
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
        /// Gets or sets the list of promoted properties for the original adapter
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

        #region Private Properties
        /// <summary>
        /// Gets the binding element instance
        /// </summary>
        private MockAdapter BindingElement
        {
            get
            {
                if (this.binding == null)
                {
                    this.binding = new MockAdapter();
                }
                
                this.binding.Encoding = this.Encoding;
                this.binding.PromotedProperties = this.PromotedProperties;

                return this.binding;
            }
        }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        /// Creates a clone of the existing BindingElement and returns it
        /// </summary>
        /// <returns>An instance of the binding element collection</returns>
        public override BindingElementCollection CreateBindingElements()
        {
            BindingElementCollection bindingElements = new BindingElementCollection();

            // Only create once
            bindingElements.Add(this.BindingElement);
            
            // Return the clone
            return bindingElements.Clone();
        }

        #endregion Public Methods

        #region Private methods

        /// <summary>
        /// Applies the current configuration to the WCFMockAdapterBindingCollectionElement
        /// </summary>
        /// <param name="configurationName">The configuration name to be used to configure the adapter binding instance with</param>
        private void ApplyConfiguration(string configurationName)
        {
            BindingsSection bindingsSection = (BindingsSection)System.Configuration.ConfigurationManager.GetSection("system.serviceModel/bindings");
            MockAdapterBindingCollectionElement bindingCollectionElement = (MockAdapterBindingCollectionElement)bindingsSection["mockBinding"];
            MockAdapterBindingElement element = bindingCollectionElement.Bindings[configurationName];

            if (element != null)
            {
                MockAdapterUtilities.Trace.Trace(
                    System.Diagnostics.TraceEventType.Information,
                    "1007", 
                    "Applying binding configuration");

                element.ApplyConfiguration(this);
            }
        }

        #endregion        
    }
}
