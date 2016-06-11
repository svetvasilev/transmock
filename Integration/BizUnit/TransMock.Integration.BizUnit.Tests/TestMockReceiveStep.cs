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

using System.IO;
using System.IO.Pipes;
using System.ServiceModel;
using System.ServiceModel.Channels;

using TransMock.TestUtils;
using TransMock.Wcf.Adapter;
using TransMock.Integration.BizUnit;

using BizUnit;
using Moq;
using BizUnit.Xaml;

namespace TransMock.Integration.BizUnit.Tests
{
    /// <summary>
    /// Tests the MockReceiveStep class
    /// </summary>
    [TestClass]
    public class TestMockReceiveStep
    {
        public TestMockReceiveStep()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        MockAdapterConnectionUri connectionUri;
        MockAdapter adapter;
        MockAdapterOutboundHandler outboundHandler;

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
        [TestInitialize()]
        public void MyTestInitialize()
        {
            //Setting up the inbound handler with all the references
            connectionUri = new MockAdapterConnectionUri(new Uri("mock://localhost/TestEndpoint"));
            adapter = new MockAdapter();
            adapter.Encoding = "UTF-8";
            MockAdapterConnectionFactory connectionFactory = new MockAdapterConnectionFactory(
                connectionUri, null, adapter);
            MockAdapterConnection connection = new MockAdapterConnection(connectionFactory);
            outboundHandler = new MockAdapterOutboundHandler(connection, null);
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup() 
        {
            //give some time for the pipe to clean
            System.Threading.Thread.Sleep(100);
        }
        
        #endregion

        [TestMethod]
        public void TestValidateMethod_ValidStep()
        {
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);
            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            //Colling Validate in order to start the 
            step.Validate(context);
            //Cleaning up the step
            step.Cleanup();
        }

        [TestMethod]
        public void TestReceiveSmallMessage_XML()
        {            
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();
            
            Context context = new Context(loggerMock.Object);

            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            //Colling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });
                        
            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);            

            outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
            
            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);            

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))), 
                Times.AtLeastOnce(), "The LogData message was not called");
            
        }

        [TestMethod]
        public void TestReceiveSmallMessage_FlatFile()
        {
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            //Colling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            string ffContent = "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;";

            Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(ffContent, Encoding.UTF8);

            outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));

            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);
            
            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s) && 
                    s == "MockReceiveStep received a message with content"),
                It.Is<string>(s => !string.IsNullOrEmpty(s) &&
                    s == "303330123333777;ABCD;00001;00002;2014-01-15;21:21:33.444;EFGH;")), 
                    Times.AtLeastOnce(), 
                    "The LogData method was not called");

        }

        [TestMethod]
        public void TestReceiveSmallMessages_Debatch3_XML()
        {
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            step.DebatchedMessageCount = 3;
            //Calling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";
                      
            for (int i = 0; i < 3; i++)
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);

                outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
            }            

            //Waiting for the manual event to be set
            if (!manualEvent.WaitOne(1000))
            {
                // The timeout elapse we wait a little bit longer
                manualEvent.WaitOne(10000);
            }

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))),
                Times.Exactly(3), "The LogData message was not called the expected number of times");

        }

        [TestMethod]
        public void TestReceiveSmallMessages_Debatch50_XML()
        {
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            step.DebatchedMessageCount = 50;
            //Calling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            for (int i = 0; i < 50; i++)
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);

                outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
            }

            //Waiting for the manual event to be set
            if (!manualEvent.WaitOne(1000))
            {
                manualEvent.WaitOne(5000);
            }

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))),
                Times.Exactly(50), 
                "The LogData method was not called");

        }

        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException))]
        public void TestReceiveSmallMessages_Debatch3_Expect2_XML()
        {
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            step.DebatchedMessageCount = 2;
            //Calling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            for (int i = 0; i < 3; i++)
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);

                outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
            }

            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))),
                Times.AtLeastOnce(), "The LogData message was not called");

        }

        [TestMethod]
        public void TestReceiveSmallMessages_Debatch2_Expect3_XML()
        {
            //Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);

            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.Timeout = 1;
            step.DebatchedMessageCount = 3;
            //Calling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be

            bool timeoutExceptionThrown = false;
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    step.Execute(context);
                    manualEvent.Set();
                }
                catch (TimeoutException)
                {
                    timeoutExceptionThrown = true;
                }
                
            });            

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            for (int i = 0; i < 2; i++)
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);

                outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
            }

            // Waiting for the manual event to be set
            // The wait time is slightly longer than the step's timeout in order to 
            // to be able to capture the TimeoutException correctly
            manualEvent.WaitOne(1200);

            Assert.IsTrue(timeoutExceptionThrown, "The expected exception was not thrown");
            
            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))),
                Times.Exactly(2), "The LogData message was not called");

        }

        [TestMethod]
        public void TestReceiveSmallMessages_Debatch3_SerialValidationSingleStep_XML()
        {
            // Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);
            BizUnit.
            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            step.DebatchedMessageCount = 3;
            
            // Setting up a validation step mock
            var validationStepMock = CreateSubStepMock();
            step.SubSteps.Add(validationStepMock.Object);

            // Calling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            for (int i = 0; i < 3; i++)
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);

                outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
            }

            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))),
                Times.AtLeastOnce(), "The LogData message was not called");

            validationStepMock.Verify(vs => vs.Execute(
                It.Is<Stream>(s => s != null),
                It.Is<Context>(c => c != null)),
                Times.Exactly(3),
                "The SubStep mock was not called the expected number of times");

        }

        [TestMethod]
        public void TestReceiveSmallMessages_Debatch3_CascadingValidationSingleStep_XML()
        {
            // Setting up the ILogger moq
            var loggerMock = CreateLoggerMock();

            Context context = new Context(loggerMock.Object);
            BizUnit.
            MockReceiveStep step = new MockReceiveStep();
            step.Url = connectionUri.Uri.OriginalString;
            step.Encoding = "UTF-8";
            step.Timeout = 30;
            step.DebatchedMessageCount = 3;
            step.ValidationMode = MultiMessageValidationMode.Cascading;

            // Setting up a validation step mock list
            var validationStepMockList = new List<Mock<global::BizUnit.Xaml.SubStepBase>>(3);

            for (int i = 0; i < 3; i++)
            {
                var validationStepMock = CreateSubStepMock();
                
                var validationStepsCollection = new System.Collections.ObjectModel.Collection<global::BizUnit.Xaml.SubStepBase>();
                validationStepsCollection.Add(validationStepMock.Object);

                step.CascadingSubSteps.Add(i, validationStepsCollection);
            }



            // Calling Validate in order to start the 
            step.Validate(context);
            //Setting up a manual reset event
            System.Threading.ManualResetEvent manualEvent = new System.Threading.ManualResetEvent(false);
            //here we queue up the step.Execute method in a separate thread as the execution model would actually be
            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                step.Execute(context);
                manualEvent.Set();
            });

            string xml = "<SomeTestMessage><Element1 attribute1=\"attributeValue\"></Element1><Element2>Some element content</Element2></SomeTestMessage>";

            for (int i = 0; i < 3; i++)
            {
                Message msg = GeneralTestHelper.CreateMessageWithBase64EncodedBody(xml, Encoding.UTF8);

                outboundHandler.Execute(msg, TimeSpan.FromSeconds(10));
            }

            //Waiting for the manual event to be set
            manualEvent.WaitOne(1000);

            loggerMock.Verify(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))),
                Times.AtLeastOnce(), "The LogData message was not called");

            foreach (var validationStepMock in validationStepMockList)
            {
                validationStepMock.Verify(vs => vs.Execute(
                    It.Is<Stream>(s => s != null),
                    It.Is<Context>(c => c != null)),
                    Times.Exactly(3),
                    "The SubStep mock was not called the expected number of times");                
            }
            

        }

        internal static Mock<ILogger> CreateLoggerMock()
        {
            Mock<ILogger> loggerMock = new Mock<ILogger>();
            
            loggerMock.Setup(l => l.LogData(
                It.Is<string>(s => !string.IsNullOrEmpty(s)),
                It.Is<string>(s => !string.IsNullOrEmpty(s))))
                .Verifiable();

            return loggerMock;
        }

        internal static Mock<SubStepBase> CreateSubStepMock()
        {
            var subStepMock = new Mock<SubStepBase>();

            subStepMock.Setup(s => s.Execute(
                It.Is<Stream>(str => str != null),
                It.Is<Context>(ctx => ctx != null)))
                .Verifiable();

            return subStepMock;
        }
    }
}
