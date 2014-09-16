using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;

using System.Reflection;
using System.ServiceModel.Configuration;
using System.Diagnostics;

using TransMock.Deploy.Utils;

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

                System.Configuration.Configuration config = ConfigurationManager.OpenMachineConfiguration();
                Debug.Assert(config != null, "Machine.Config returned null");

                MachineConfigManager.AddMachineConfigurationInfo(
                    this.Context.Parameters[INSTALLER_PARM_INSTALLDIR], config);

                if (System.Environment.Is64BitOperatingSystem)
                {
                    //For 64-Bit operating system there should be updated the 64-bit machine config as well
                    string machineConfigPathFor64Bit = System.Runtime.InteropServices.RuntimeEnvironment
                        .GetRuntimeDirectory().Replace("Framework", "Framework64");

                    ConfigurationFileMap configMap = new ConfigurationFileMap(
                        System.IO.Path.Combine(machineConfigPathFor64Bit,
                            "Config","machine.config"));

                    config = ConfigurationManager.OpenMappedMachineConfiguration(configMap);
                    Debug.Assert(config != null, "Machine.Config for 64-bit returned null");

                    MachineConfigManager.AddMachineConfigurationInfo(
                        this.Context.Parameters[INSTALLER_PARM_INSTALLDIR], config);
                }
                
            }
            catch (Exception ex)
            {
                throw new InstallException("Error while adding adapter configuration information. " + ex.Message);
            }
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
                System.Configuration.Configuration config = ConfigurationManager.OpenMachineConfiguration();
                Debug.Assert(config != null, "Machine.Config returned null");

                MachineConfigManager.RemoveMachineConfigurationInfo(config);

                if (System.Environment.Is64BitOperatingSystem)
                {
                    //For 64-Bit operating system there should be updated the 64-bit machine config as well
                    string machineConfigPathFor64Bit = System.Runtime.InteropServices.RuntimeEnvironment
                        .GetRuntimeDirectory().Replace("Framework", "Framework64");

                    ConfigurationFileMap configMap = new ConfigurationFileMap(
                        System.IO.Path.Combine(machineConfigPathFor64Bit,
                            "Config", "machine.config"));

                    config = ConfigurationManager.OpenMappedMachineConfiguration(configMap);
                    Debug.Assert(config != null, "Machine.Config for 64-bit returned null");

                    MachineConfigManager.RemoveMachineConfigurationInfo(config);
                }
            }
            catch (Exception ex)
            {
                throw new InstallException("Error while removing adapter configuration information" + ex.Message);
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
