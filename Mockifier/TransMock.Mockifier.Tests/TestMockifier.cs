using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransMock.Mockifier;

namespace TransMock.Mockifier.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestMockifier
    {
        public TestMockifier()
        {
            //
            // TODO: Add constructor logic here
            //
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
        [DeploymentItem(@"TestData\TestApplication.BindingInfo.xml")]
        public void Execute_InputBindingsOnly()
        {
            //We copy and rename the input file
            System.IO.File.Copy("TestApplication.BindingInfo.xml", "TestApplication.BindingInfo_in.xml");
            //We execute the mockifier with the desired parameters
            var processInfo = new ProcessStartInfo()
            {
                FileName = "mockifier.exe",
                Arguments = "-b TestApplication.BindingInfo_in.xml",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string result = null;
            using (Process p = Process.Start(processInfo))
            {
                 result = p.StandardOutput.ReadToEnd();
                 p.WaitForExit();
            }

            Assert.AreEqual("Bindings mockified successfully!", result, "Output was wrong!");
        }
    }
}
