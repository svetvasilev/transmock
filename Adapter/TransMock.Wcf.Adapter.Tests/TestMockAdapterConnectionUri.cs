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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TransMock.Wcf.Adapter.Tests
{
    /// <summary>
    /// Summary description for TestMockAdapterConnectionUri
    /// </summary>
    [TestClass]
    public class TestMockAdapterConnectionUri
    {
        public TestMockAdapterConnectionUri()
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
        public void TestSettingUri()
        {
            MockAdapterConnectionUri uri = new MockAdapterConnectionUri();
            uri.Uri = new Uri("mock://localhost/SomeEndpoint");

            Assert.AreEqual("localhost", uri.Host, "Host is not set properly");
            Assert.AreEqual("SomeEndpoint", uri.SystemEndpoint, "SystemEndpoing is not set properly");
            Assert.IsNull(uri.Operation, "Operation is not set properly");
        }

        [TestMethod]
        public void TestSettingUri_WithOperation()
        {
            MockAdapterConnectionUri uri = new MockAdapterConnectionUri();
            uri.Uri = new Uri("mock://localhost/SomeEndpoint/SomeOperation");

            Assert.AreEqual("localhost", uri.Host, "Host is not set properly");
            Assert.AreEqual("SomeEndpoint", uri.SystemEndpoint, "SystemEndpoing is not set properly");
            Assert.AreEqual("SomeOperation", uri.Operation, "Operation is not set properly");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSettingUri_NoEndpoint()
        {
            MockAdapterConnectionUri uri = new MockAdapterConnectionUri();
            uri.Uri = new Uri("mock://localhost");            
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestSettingUri_WrongScheme()
        {
            MockAdapterConnectionUri uri = new MockAdapterConnectionUri();
            uri.Uri = new Uri("mack://localhost/SomeEndpoint");
        }

        [TestMethod]
        public void TestGettingUri()
        {
            MockAdapterConnectionUri uri = new MockAdapterConnectionUri();
            uri.Host = "localhost";
            uri.SystemEndpoint = "SomeEndpoint";
            
            Assert.AreEqual("mock://localhost/SomeEndpoint", uri.Uri.OriginalString, "The URI is not correct");
        }

        [TestMethod]
        public void TestGettingUri_WithOperation()
        {
            MockAdapterConnectionUri uri = new MockAdapterConnectionUri();
            uri.Host = "localhost";
            uri.SystemEndpoint = "SomeEndpoint";
            uri.Operation = "SomeOperation";

            Assert.AreEqual("mock://localhost/SomeEndpoint/SomeOperation", uri.Uri.OriginalString, "The URI is not correct");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestGettingUri_NoHost()
        {
            MockAdapterConnectionUri uri = new MockAdapterConnectionUri();
            uri.SystemEndpoint = "SomeEndpoint";
            uri.Operation = "SomeOperation";

            Uri resultUri = uri.Uri;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestGettingUri_NoSystemEndpoint()
        {
            MockAdapterConnectionUri uri = new MockAdapterConnectionUri();
            uri.Host = "localhost";                       

            Uri resultUri = uri.Uri;
        }
    }
}
