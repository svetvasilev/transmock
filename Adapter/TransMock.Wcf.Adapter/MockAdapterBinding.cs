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
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    public class MockAdapterBinding : AdapterBinding
    {
        // Scheme in Binding does not have to be the same as Adapter Scheme. 
        // Over write this value as appropriate.
        private const string BindingScheme = "mock";

        /// <summary>
        /// Initializes a new instance of the AdapterBinding class
        /// </summary>
        public MockAdapterBinding() { }

        /// <summary>
        /// Initializes a new instance of the AdapterBinding class with a configuration name
        /// </summary>
        public MockAdapterBinding(string configName)
        {
            ApplyConfiguration(configName);
        }

        /// <summary>
        /// Applies the current configuration to the WCFMockAdapterBindingCollectionElement
        /// </summary>
        private void ApplyConfiguration(string configurationName)
        {   
            BindingsSection bindingsSection = (BindingsSection)System.Configuration.ConfigurationManager.GetSection("system.serviceModel/bindings");
            MockAdapterBindingCollectionElement bindingCollectionElement = (MockAdapterBindingCollectionElement)bindingsSection["mockBinding"];
            MockAdapterBindingElement element = bindingCollectionElement.Bindings[configurationName];
            if (element != null)
            {
                MockAdapterUtilities.Trace.Trace(System.Diagnostics.TraceEventType.Information,
                    "1007", "Applying binding configuration");

                element.ApplyConfiguration(this);
            }
            
         }



        #region Private Fields

        private MockAdapter binding;

        #endregion Private Fields

        #region Custom Generated Fields        

        private string host;

        private string systemEndpoint;

        private string operation;

        private string encoding;

        private string promotedProperties;

        #endregion Custom Generated Fields

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

        private MockAdapter BindingElement
        {
            get
            {
                if (binding == null)
                    binding = new MockAdapter();
                
                binding.Encoding = this.Encoding;
                binding.PromotedProperties = this.PromotedProperties;
                return binding;
            }
        }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        /// Creates a clone of the existing BindingElement and returns it
        /// </summary>
        public override BindingElementCollection CreateBindingElements()
        {
            BindingElementCollection bindingElements = new BindingElementCollection();
            //Only create once
            bindingElements.Add(this.BindingElement);
            //Return the clone
            return bindingElements.Clone();
        }

        #endregion Public Methods

    }
}
