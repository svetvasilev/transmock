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
/// Module      :  MockAdapterBindingElement.cs
/// Description :  Provides a base class for the configuration elements.
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    /// <summary>
    /// The mock adapter binding element class
    /// </summary>
    public class MockAdapterBindingElement : StandardBindingElement
    {
        /// <summary>
        /// The collection of configuration properties for the binding element
        /// </summary>
        private ConfigurationPropertyCollection properties;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterBindingElement"/> class
        /// </summary>
        public MockAdapterBindingElement()
            : base(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterBindingElement"/> class with a configuration name
        /// </summary>
        /// <param name="configurationName">The configuration name to be used to configure the adapter binding instance with</param>
        public MockAdapterBindingElement(string configurationName)
            : base(configurationName)
        {
        }

        #endregion Constructors

        #region Custom Generated Properties

        /// <summary>
        /// Gets or sets the encoding used for message serialization
        /// </summary>
        [System.Configuration.ConfigurationProperty("Encoding")]
        public string Encoding
        {
            get
            {
                return (string)base["Encoding"];
            }

            set
            {
                base["Encoding"] = value;
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
                return (string)base["PromotedProperties"];
            }

            set
            {
                base["PromotedProperties"] = value;
            }
        }

        #endregion Custom Generated Properties

        #region Protected Properties

        /// <summary>
        /// Gets the type of the BindingElement
        /// </summary>
        protected override Type BindingElementType
        {
            get
            {
                return typeof(MockAdapterBinding);
            }
        }

        #endregion Protected Properties

        #region StandardBindingElement Members
        /// <summary>
        /// Gets a collection of the configuration properties
        /// </summary>        
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (this.properties == null)
                {
                    ConfigurationPropertyCollection configProperties = base.Properties;

                    configProperties.Add(new ConfigurationProperty(
                        "Encoding", typeof(string), null, null, null, ConfigurationPropertyOptions.None));
                    configProperties.Add(new ConfigurationProperty(
                        "PromotedProperties", typeof(string), null, null, null, ConfigurationPropertyOptions.None));
                    
                    this.properties = configProperties;
                }

                return this.properties;
            }
        }

        /// <summary>
        /// Initializes the binding with the configuration properties
        /// </summary>
        /// <param name="binding">The binding instance</param>
        protected override void InitializeFrom(Binding binding)
        {
            base.InitializeFrom(binding);
            MockAdapterBinding adapterBinding = (MockAdapterBinding)binding;
            
            this["Encoding"] = adapterBinding.Encoding;
            this["PromotedProperties"] = adapterBinding.PromotedProperties;
        }

        /// <summary>
        /// Applies the configuration
        /// </summary>
        /// <param name="binding">The binding instance</param>
        protected override void OnApplyConfiguration(Binding binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException("binding");
            }

            MockAdapterBinding adapterBinding = (MockAdapterBinding)binding;
            
            adapterBinding.Encoding = (string)this["Encoding"];
            adapterBinding.PromotedProperties = (string)this["PromotedProperties"];
        }

        #endregion StandardBindingElement Members
    }
}
