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

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Configuration;
//using System.Configuration.Install;
using System.ServiceModel.Configuration;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace TransMock.Deploy.CustomActions.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestTransMockInstaller
    {
        public TestTransMockInstaller()
        {            
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestInstall()
        {
            TransMockInstaller.TestAddConfiguration(@"..\..\..\Adapter\TransMock.Wcf.Adapter\bin\Debug");
            
            Configuration config = ConfigurationManager.OpenMachineConfiguration();
            Assert.IsNotNull(config , "Machine.Config returned null");

            ServiceModelSectionGroup sectionGroup = config.GetSectionGroup("system.serviceModel") as ServiceModelSectionGroup;

            //Assert.IsTrue(sectionGroup.Client.Endpoints.ContainsKey("mockBinding"), "The mockBinding is not present in the machine.config");
            Assert.IsTrue(sectionGroup.Extensions.BindingElementExtensions.ContainsKey("mockTransport"), "The mockBinding element extension is not present in the machine.config");
            Assert.IsTrue(sectionGroup.Extensions.BindingExtensions.ContainsKey("mockBinding"), "The mockBinding extention is not present in the machine.config");
        }

        [TestMethod]        
        public void TestUninstall()
        {
            TransMockInstaller.TestRemoveConfiguration(@"..\..\..\Adapter\TransMock.Wcf.Adapter\bin\Debug");

            Configuration config = ConfigurationManager.OpenMachineConfiguration();
            Assert.IsNotNull(config, "Machine.Config returned null");

            ServiceModelSectionGroup sectionGroup = config.GetSectionGroup("system.serviceModel") as ServiceModelSectionGroup;
            
            Assert.IsFalse(sectionGroup.Extensions.BindingElementExtensions.ContainsKey("mockTransport"), "The mockBinding element extension is still present in the machine.config");
            Assert.IsFalse(sectionGroup.Extensions.BindingExtensions.ContainsKey("mockBinding"), "The mockBinding extention is still present in the machine.config");
        }
    }
}
