using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel;
using System.Configuration;

using System.Reflection;
using System.ServiceModel.Configuration;
using System.Diagnostics;

namespace TransMock.Deploy.Utils
{
    public static class MachineConfigManager
    {
        const string BINDING_ASSEMBLY_NAME = "TransMock.Wcf.Adapter.dll";
        const string BINDINGELEM_NAME = "mockTransport";
        const string BINDINGELEM_TYPE = "TransMock.Wcf.Adapter.MockAdapterBindingElementExtensionElement";
        const string BINDING_NAME = "mockBinding";
        const string BINDING_TYPE = "TransMock.Wcf.Adapter.MockAdapterBindingCollectionElement";
        const string BINDING_SCHEME = "mock";
        /// <summary>
        /// Registers the adapter with the WCF configuration
        /// </summary>
        public static void AddMachineConfigurationInfo(string installDir,
            System.Configuration.Configuration config)
        {
            Assembly adapterAssembly;
            Type bindingSectionType;
            Type bindingElementExtensionType;

            string path = System.IO.Path.Combine(installDir, BINDING_ASSEMBLY_NAME);
            adapterAssembly = Assembly.LoadFrom(path);

            Debug.Assert(adapterAssembly != null, "Adapter assembly is null.");

            bindingSectionType = adapterAssembly.GetType(BINDING_TYPE, true);
            Debug.Assert(bindingSectionType != null, "Binding type is null.");

            bindingElementExtensionType = adapterAssembly.GetType(BINDINGELEM_TYPE, true);
            Debug.Assert(bindingElementExtensionType != null, "Binding element extension type is null.");

            // add <client><endpoint>             
            ServiceModelSectionGroup sectionGroup = config.GetSectionGroup("system.serviceModel") as ServiceModelSectionGroup;
            if (sectionGroup != null)
            {

                bool channelEndpointElementExists = false;
                // this call can throw an exception if there is problem 
                // loading endpoint configurations - e.g. each endpoint
                // tries to load binding which in turn loads the DLL
                ClientSection clientSection = sectionGroup.Client;
                foreach (ChannelEndpointElement elem in clientSection.Endpoints)
                {
                    if (elem.Binding.Equals(BINDING_NAME, StringComparison.OrdinalIgnoreCase) &&
                        elem.Name.Equals(BINDING_SCHEME, StringComparison.OrdinalIgnoreCase) &&
                        elem.Contract.Equals("IMetadataExchange", StringComparison.OrdinalIgnoreCase))
                    {
                        channelEndpointElementExists = true;
                        break;
                    }
                }
                if (!channelEndpointElementExists)
                {
                    Debug.WriteLine("Adding ChannelEndpointElement for : " + BINDING_NAME);

                    ChannelEndpointElement elem = new ChannelEndpointElement();
                    elem.Binding = BINDING_NAME;
                    elem.Name = BINDING_SCHEME;
                    elem.Contract = "IMetadataExchange";
                    sectionGroup.Client.Endpoints.Add(elem);

                    Debug.WriteLine("Added ChannelEndpointElement for : " + BINDING_NAME);
                }

                // add <bindingElementExtension>
                if (!sectionGroup.Extensions.BindingElementExtensions.ContainsKey(BINDINGELEM_NAME))
                {
                    ExtensionElement ext = new ExtensionElement(BINDINGELEM_NAME,
                        bindingElementExtensionType.FullName + ", " + bindingElementExtensionType.Assembly.FullName);
                    sectionGroup.Extensions.BindingElementExtensions.Add(ext);
                }

                // add <bindingExtension>
                if (!sectionGroup.Extensions.BindingExtensions.ContainsKey(BINDING_NAME))
                {
                    ExtensionElement ext = new ExtensionElement(BINDING_NAME,
                        bindingSectionType.FullName + ", " + bindingSectionType.Assembly.FullName);
                    sectionGroup.Extensions.BindingExtensions.Add(ext);
                }

                config.Save();
            }
            else
                throw new ApplicationException("Machine.Config doesn't contain system.serviceModel node");
        }

        /// <summary>
        /// Unregisters the adapter with WCF configuration
        /// </summary>
        public static void RemoveMachineConfigurationInfo(System.Configuration.Configuration config)
        {
            ServiceModelSectionGroup sectionGroup = config.GetSectionGroup("system.serviceModel") as ServiceModelSectionGroup;
            ChannelEndpointElement elemToRemove = null;

            if (sectionGroup != null)
            {
                // Remove <client><endpoint>
                foreach (ChannelEndpointElement elem in sectionGroup.Client.Endpoints)
                {
                    if (elem.Binding.Equals(BINDING_NAME, StringComparison.OrdinalIgnoreCase) &&
                        elem.Name.Equals(BINDING_SCHEME, StringComparison.OrdinalIgnoreCase) &&
                        elem.Contract.Equals("IMetadataExchange", StringComparison.OrdinalIgnoreCase))
                    {
                        elemToRemove = elem;
                        break;
                    }
                }
                if (elemToRemove != null)
                {
                    Debug.WriteLine("Removing ChannelEndpointElement for : " + BINDING_NAME);

                    sectionGroup.Client.Endpoints.Remove(elemToRemove);

                    Debug.WriteLine("Removed ChannelEndpointElement for : " + BINDING_NAME);
                }
                // Remove <bindingExtension> for this adapter
                if (sectionGroup.Extensions.BindingExtensions.ContainsKey(BINDING_NAME))
                {
                    sectionGroup.Extensions.BindingExtensions.RemoveAt(BINDING_NAME);
                }
                // Remove <bindingElementExtension> for this adapter
                if (sectionGroup.Extensions.BindingElementExtensions.ContainsKey(BINDINGELEM_NAME))
                {
                    sectionGroup.Extensions.BindingElementExtensions.RemoveAt(BINDINGELEM_NAME);
                }

                config.Save();
            }
            else
            {
                throw new ApplicationException("Machine.Config doesn't contain system.serviceModel node");
            }
        }

    }
}
