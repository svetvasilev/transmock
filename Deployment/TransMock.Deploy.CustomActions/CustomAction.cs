using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Configuration;

using TransMock.Deploy.Utils;

using Microsoft.Deployment.WindowsInstaller;

namespace TestCustomAction
{
    /// <summary>
    /// Contains the custom actions for the WIX installer
    /// </summary>
    public class CustomActions
    {
        const string INSTALLER_PARM_INSTALLDIR = "TRANSMOCKFOLDER"; 

        [CustomAction]
        public static ActionResult AfterInstall(Session session)
        {
            session.Log("Begin AfterInstall");

            try
            {
                Debug.Assert(session != null, "Session foro this installation is null.");

                System.Configuration.Configuration config = ConfigurationManager.OpenMachineConfiguration();
                Debug.Assert(config != null, "Machine.Config returned null");

                MachineConfigManager.AddMachineConfigurationInfo(
                    session[INSTALLER_PARM_INSTALLDIR], config);

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

                    MachineConfigManager.AddMachineConfigurationInfo(
                        session[INSTALLER_PARM_INSTALLDIR], config);
                }

                session.Log("End AfterInstall with success.");

                return ActionResult.Success;

            }
            catch (Exception ex)
            {
                session.Log("Error while adding adapter configuration information. " + ex.Message);
                session.Log("End AfterInstall with error.");

                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult BeforeUninstall(Session session)
        {
            session.Log("Begin BeforeUninstall");

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

                session.Log("End BeforeUninstall with success.");

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("Error while removing adapter configuration information" + ex.Message);
                session.Log("End BeforeUninstall with error.");

                return ActionResult.Failure;
            }
        }
    }
}
