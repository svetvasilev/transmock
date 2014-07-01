/// -----------------------------------------------------------------------------------------------------------
/// Module      :  WCFMockAdapterBindingElement.cs
/// Description :  Provides a base class for the configuration elements.
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
    public class MockAdapterBindingElement : StandardBindingElement
    {
        private ConfigurationPropertyCollection properties;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the WCFMockAdapterBindingElement class
        /// </summary>
        public MockAdapterBindingElement()
            : base(null)
        {
        }


        /// <summary>
        /// Initializes a new instance of the WCFMockAdapterBindingElement class with a configuration name
        /// </summary>
        public MockAdapterBindingElement(string configurationName)
            : base(configurationName)
        {
        }

        #endregion Constructors

        #region Custom Generated Properties

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
        /// Initializes the binding with the configuration properties
        /// </summary>
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
        protected override void OnApplyConfiguration(Binding binding)
        {
            if (binding == null)
                throw new ArgumentNullException("binding");

            MockAdapterBinding adapterBinding = (MockAdapterBinding)binding;
            
            adapterBinding.Encoding = (System.String)this["Encoding"];
            adapterBinding.PromotedProperties = (System.String)this["PromotedProperties"];
        }

        /// <summary>
        /// Returns a collection of the configuration properties
        /// </summary>
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (this.properties == null)
                {
                    ConfigurationPropertyCollection configProperties = base.Properties;
                    
                    configProperties.Add(new ConfigurationProperty("Encoding", typeof(System.String), null, null, null, ConfigurationPropertyOptions.None));
                    configProperties.Add(new ConfigurationProperty("PromotedProperties", typeof(System.String), null, null, null, ConfigurationPropertyOptions.None));
                    this.properties = configProperties;
                }
                return this.properties;
            }
        }


        #endregion StandardBindingElement Members
    }
}
