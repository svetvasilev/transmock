/// -----------------------------------------------------------------------------------------------------------
/// Module      :  WCFMockAdapterBindingElementExtensionElement.cs
/// Description :  This class is provided to surface Adapter as a binding element, so that it 
///                can be used within a user-defined WCF "Custom Binding".
///                In configuration file, it is defined under
///                <system.serviceModel>
///                  <extensions>
///                     <bindingElementExtensions>
///                         <add name="{name}" type="{this}, {assembly}"/>
///                     </bindingElementExtensions>
///                  </extensions>
///                </system.serviceModel>
/// -----------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Channels;
using System.Configuration;
using System.Globalization;

using Microsoft.ServiceModel.Channels.Common;
#endregion

namespace TransMock.Wcf.Adapter
{
    using System;
    using System.Configuration;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;

    public class MockAdapterBindingElementExtensionElement : BindingElementExtensionElement
    {

        #region  Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public MockAdapterBindingElementExtensionElement()
        {
        }

        #endregion Constructor

        #region Custom Generated Properties

        [System.Configuration.ConfigurationProperty("uRI", DefaultValue = "mock://")]
        public string URI
        {
            get
            {
                return ((string)(base["URI"]));
            }
            set
            {
                base["URI"] = value;
            }
        }



        [System.Configuration.ConfigurationProperty("Encoding")]
        public string Encoding
        {
            get
            {
                return ((string)(base["Encoding"]));
            }
            set
            {
                base["Encoding"] = value;
            }
        }



        [System.Configuration.ConfigurationProperty("PromotedProperties")]
        public string PromotedProperties
        {
            get
            {
                return ((string)(base["PromotedProperties"]));
            }
            set
            {
                base["PromotedProperties"] = value;
            }
        }

        #endregion Custom Generated Properties

        #region BindingElementExtensionElement Methods
        /// <summary>
        /// Return the type of the adapter (binding element)
        /// </summary>
        public override Type BindingElementType
        {
            get
            {
                return typeof(MockAdapter);
            }
        }
        /// <summary>
        /// Returns a collection of the configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection configProperties = base.Properties;
                configProperties.Add(new ConfigurationProperty("URI", typeof(System.String), "mock://", null, null, ConfigurationPropertyOptions.None));
                configProperties.Add(new ConfigurationProperty("Encoding", typeof(System.String), null, null, null, ConfigurationPropertyOptions.None));
                configProperties.Add(new ConfigurationProperty("PromotedProperties", typeof(System.String), null, null, null, ConfigurationPropertyOptions.None));
                return configProperties;
            }
        }

        /// <summary>
        /// Instantiate the adapter.
        /// </summary>
        /// <returns></returns>
        protected override BindingElement CreateBindingElement()
        {
            MockAdapter adapter = new MockAdapter();
            this.ApplyConfiguration(adapter);
            return adapter;
        }

        /// <summary>
        /// Apply the configuration properties to the adapter.
        /// </summary>
        /// <param name="bindingElement"></param>
        public override void ApplyConfiguration(BindingElement bindingElement)
        {
            base.ApplyConfiguration(bindingElement);
            MockAdapter adapterBinding = ((MockAdapter)(bindingElement));
            //adapterBinding.URI = (System.String)this["URI"];
            adapterBinding.Encoding = (System.String)this["Encoding"];
            adapterBinding.PromotedProperties = (System.String)this["PromotedProperties"];
        }

        /// <summary>
        /// Initialize the binding properties from the adapter.
        /// </summary>
        /// <param name="bindingElement"></param>
        protected override void InitializeFrom(BindingElement bindingElement)
        {
            base.InitializeFrom(bindingElement);
            MockAdapter adapterBinding = ((MockAdapter)(bindingElement));
            //this["URI"] = adapterBinding.URI;
            this["Encoding"] = adapterBinding.Encoding;
            this["PromotedProperties"] = adapterBinding.PromotedProperties;
        }

        /// <summary>
        /// Copy the properties to the custom binding
        /// </summary>
        /// <param name="from"></param>
        public override void CopyFrom(ServiceModelExtensionElement from)
        {
            base.CopyFrom(from);
            MockAdapterBindingElementExtensionElement adapterBinding = ((MockAdapterBindingElementExtensionElement)(from));
            //this["URI"] = adapterBinding.URI;
            this["Encoding"] = adapterBinding.Encoding;
            this["PromotedProperties"] = adapterBinding.PromotedProperties;
        }

        #endregion BindingElementExtensionElement Methods
    }
}

