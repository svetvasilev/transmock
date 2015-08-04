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
/// Module      :  MockAdapterBindingElementExtensionElement.cs
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
    using System;
    using System.Configuration;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;

    /// <summary>
    /// The mock adapter binding element extension class.This class is provided to surface Adapter as a binding element, so that it 
    ///                can be used within a user-defined WCF "Custom Binding".
    ///                In configuration file, it is defined under
    ///                <system.serviceModel>
    ///                  <extensions>
    ///                     <bindingElementExtensions>
    ///                         <add name="{name}" type="{this}, {assembly}"/>
    ///                     </bindingElementExtensions>
    ///                  </extensions>
    ///                </system.serviceModel>    
    /// </summary>
    public class MockAdapterBindingElementExtensionElement : BindingElementExtensionElement
    {
        #region  Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAdapterBindingElementExtensionElement"/> class with the WCFMockAdapterConnectionFactory
        /// </summary>
        public MockAdapterBindingElementExtensionElement()
        {
        }

        #endregion Constructor

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

        #region BindingElementExtension Properties
        /// <summary>
        /// Gets the type of the adapter (binding element)
        /// </summary>
        public override Type BindingElementType
        {
            get
            {
                return typeof(MockAdapter);
            }
        }

        /// <summary>
        /// Gets a collection of the configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection configProperties = base.Properties;

                configProperties.Add(new ConfigurationProperty(
                    "Encoding", typeof(string), null, null, null, ConfigurationPropertyOptions.None));
                configProperties.Add(new ConfigurationProperty(
                    "PromotedProperties", typeof(string), null, null, null, ConfigurationPropertyOptions.None));

                return configProperties;
            }
        }
        #endregion

        #region BindingElementExtensionElement Methods
        /// <summary>
        /// Apply the configuration properties to the adapter.
        /// </summary>
        /// <param name="bindingElement">The binding element containing the adapter configuration</param>
        public override void ApplyConfiguration(BindingElement bindingElement)
        {
            base.ApplyConfiguration(bindingElement);
            MockAdapter adapterBinding = (MockAdapter)bindingElement;

            adapterBinding.Encoding = (string)this["Encoding"];
            adapterBinding.PromotedProperties = (string)this["PromotedProperties"];
        }

        /// <summary>
        /// Copy the properties to the custom binding
        /// </summary>
        /// <param name="from">The instance of the custom binding</param>
        public override void CopyFrom(ServiceModelExtensionElement from)
        {
            base.CopyFrom(from);
            MockAdapterBindingElementExtensionElement adapterBinding = (MockAdapterBindingElementExtensionElement)from;

            this["Encoding"] = adapterBinding.Encoding;
            this["PromotedProperties"] = adapterBinding.PromotedProperties;
        }

        /// <summary>
        /// Instantiate the adapter.
        /// </summary>
        /// <returns>An instance of the binding element representing the adapter</returns>
        protected override BindingElement CreateBindingElement()
        {
            MockAdapter adapter = new MockAdapter();
            this.ApplyConfiguration(adapter);

            return adapter;
        }

        /// <summary>
        /// Initialize the binding properties from the adapter.
        /// </summary>
        /// <param name="bindingElement">The binding element containing the adapter configuration</param>
        protected override void InitializeFrom(BindingElement bindingElement)
        {
            base.InitializeFrom(bindingElement);
            MockAdapter adapterBinding = (MockAdapter)bindingElement;

            this["Encoding"] = adapterBinding.Encoding;
            this["PromotedProperties"] = adapterBinding.PromotedProperties;
        }

        #endregion BindingElementExtensionElement Methods
    }
}