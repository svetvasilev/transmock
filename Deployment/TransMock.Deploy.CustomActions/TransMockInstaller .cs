using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;

using System.Reflection;
using System.ServiceModel.Configuration;
using System.Diagnostics;


namespace TransMock.Deploy.CustomActions
{
    //Custom action to register the adapter with WCF configuration in machine.config 

    //<system.serviceModel>
    //  <extensions>
    //    <bindingElementExtensions>
    //      <add name="{BINDINGELEM_NAME}" type="{BINDINGELEM_TYPE}, {Assembly Information}" />
    //    </bindingElementExtensions>
    //    <bindingExtensions>
    //      <add name="{BINDING_NAME}" type="{BINDING_TYPE}, {Assembly Information}" />
    //    </bindingExtensions>
    //  </extensions>
    //  <client>
    //    <endpoint binding="{BINDING_NAME}" contract="IMetadataExchange" name="{BINDING_SCHEME}" />
    //  </client>
    //</system.serviceModel>

    [RunInstaller(true)]
    public partial class TransMockInstaller : Installer
    {
        private Assembly adapterAssembly;
        private Type bindingSectionType;
        private Type bindingElementExtensionType;
        const string INSTALLER_PARM_INSTALLDIR = "INSTALLDIR";
        const string BINDING_ASSEMBLY_NAME = "TransMock.Wcf.Adapter.dll";
        const string BINDINGELEM_NAME = "mockTransport";
        const string BINDINGELEM_TYPE = "TransMock.Wcf.Adapter.MockAdapterBindingElementExtensionElement";
        const string BINDING_NAME = "mockBinding";
        const string BINDING_TYPE = "TransMock.Wcf.Adapter.MockAdapterBindingCollectionElement";
        const string BINDING_SCHEME = "mock";

        /// <summary>
        /// Constructor - initialize the components and register the event handlers
        /// </summary>
        public TransMockInstaller()
        {
            //InitializeComponent();
            this.AfterInstall += new InstallEventHandler(AfterInstallEventHandler);
            this.BeforeUninstall += new InstallEventHandler(BeforeUninstallEventHandler);
        }

        /// <summary>
        /// Add the WCF configuration information in machine.config when installing the adapter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AfterInstallEventHandler(object sender, InstallEventArgs e)
        {
            try
            {
                Debug.Assert(this.Context != null, "Context of this installation is null.");
                
                string path = System.IO.Path.Combine(this.Context.Parameters[INSTALLER_PARM_INSTALLDIR], BINDING_ASSEMBLY_NAME);
                adapterAssembly = Assembly.LoadFrom(path);
                
                Debug.Assert(adapterAssembly != null, "Adapter assembly is null.");
                
                bindingSectionType = adapterAssembly.GetType(BINDING_TYPE, true);
                Debug.Assert(bindingSectionType != null, "Binding type is null.");
                
                bindingElementExtensionType = adapterAssembly.GetType(BINDINGELEM_TYPE, true);
                Debug.Assert(bindingElementExtensionType != null, "Binding element extension type is null.");
                
                AddMachineConfigurationInfo();
            }
            catch (Exception ex)
            {
                throw new InstallException("Error while adding adapter configuration information. " + ex.Message);
            }
        }

        /// <summary>
        /// Registers the adapter with the WCF configuration
        /// NOTE: The 
        /// </summary>
        public void AddMachineConfigurationInfo()
        {
            System.Configuration.Configuration config = ConfigurationManager.OpenMachineConfiguration();
            Debug.Assert(config != null, "Machine.Config returned null");
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
                throw new InstallException("Machine.Config doesn't contain system.serviceModel node");
        }

        /// <summary>
        /// Remove the machine configuration information when uninstalling the adapter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BeforeUninstallEventHandler(object sender, InstallEventArgs e)
        {
            try
            {
                RemoveMachineConfigurationInfo();
            }
            catch (Exception ex)
            {
                throw new InstallException("Error while removing adapter configuration information" + ex.Message);
            }
        }

        /// <summary>
        /// Unregisters the adapter with WCF configuration
        /// </summary>
        public void RemoveMachineConfigurationInfo()
        {
            Configuration config = ConfigurationManager.OpenMachineConfiguration();
            Debug.Assert(config != null, "Machine.Config returned null");
            
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
                throw new InstallException("Machine.Config doesn't contain system.serviceModel node");
            }
        }

        /// <summary>
        /// Use this for testing outside of the Setup project
        /// </summary>
        /// <param name="assemblyDirectory">Directory where the adapter assembly is located</param>
        public static void TestAddConfiguration(string assemblyDirectory)
        {
            TransMockInstaller action = new TransMockInstaller();
            InstallContext context = new InstallContext();
            // In the Setup project, this is set by selecting custom action
            // and in Properties setting /INSTALLDIR="[TARGETDIR]\" for CustomActionData
            context.Parameters.Add("INSTALLDIR", assemblyDirectory);
            action.Context = context;
            action.AfterInstallEventHandler(null, null);
        }

        /// <summary>
        /// Use this for testing outside of the Setup project
        /// </summary>
        /// <param name="assemblyDirectory">Directory where the adapter assembly is located</param>
        public static void TestRemoveConfiguration(string assemblyDirectory)
        {
            TransMockInstaller action = new TransMockInstaller();
            InstallContext context = new InstallContext();
            // In the Setup project, this is set by selecting custom action
            // and in Properties setting /INSTALLDIR="[TARGETDIR]\" for CustomActionData
            context.Parameters.Add("INSTALLDIR", assemblyDirectory);
            action.Context = context;
            action.BeforeUninstallEventHandler(null, null);
        }
    }
    
}
