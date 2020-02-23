using System;

using BizUnit;
using TransMock.Integration.BizUnit;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizTalkTests.IntegrationTests
{
    [TestClass]
    public class TestDynamicSendPort
    {
        [TestInitialize]
        public void TestSetup()
        {
            TransMockExecutionBeacon.Start();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TransMockExecutionBeacon.Stop();
        }

        [TestMethod]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        public void TestHappyPath_OneWay()
        {
            var testCase = new BizUnit.Core.TestBuilder.TestCase();

            var inMsgStep = new MockSendStep()
            {
                Url = "mock://localhost/BTS.OneWayReceive_FILE",
                RequestPath = "StartMessage.xml",
                Encoding = "UTF-8"
            };

            testCase.ExecutionSteps.Add(inMsgStep);

            var outMsgStep = new MockReceiveStep()
            {
                Url = "mock://localhost/DynamicPortOut",
                Encoding = "UTF-8",
                Timeout = 10
            };

            testCase.ExecutionSteps.Add(outMsgStep);

            BizUnit.Core.TestRunner testRunner = new BizUnit.Core.TestRunner(testCase);

            testRunner.Run();
        }

        [TestMethod]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        public void TestHappyPath_OneWay_HelperClass()
        {
            var testCase = new BizUnit.Core.TestBuilder.TestCase();

            var inMsgStep = new MockSendStep()
            {
                Url = BizTalkTestsOldMockAddresses.BTS_OneWayReceive_FILE,
                RequestPath = "StartMessage.xml",
                Encoding = "UTF-8"
            };

            testCase.ExecutionSteps.Add(inMsgStep);

            var outMsgStep = new MockReceiveStep()
            {
                Url = BizTalkTestsOldMockAddresses.DynamicPortOut,
                Encoding = "UTF-8",
                Timeout = 10
            };

            testCase.ExecutionSteps.Add(outMsgStep);

            BizUnit.Core.TestRunner testRunner = new BizUnit.Core.TestRunner(testCase);

            testRunner.Run();
        }

        [TestMethod]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        [DeploymentItem(@"TestData\ResponseMessage.xml")]
        public void TestHappyPath_TwoWay_HelperClass()
        {
            var testCase = new BizUnit.Core.TestBuilder.TestCase();

            var inMsgStep = new MockSendStep()
            {
                Url = BizTalkTestsOldMockAddresses.BTS_OneWayReceive2_FILE,
                RequestPath = "StartMessage.xml",
                Encoding = "UTF-8"
            };

            testCase.ExecutionSteps.Add(inMsgStep);

            var outMsgStep = new MockRequestResponseStep()
            {
                Url = BizTalkTestsOldMockAddresses.DynamicPortOut2Way,                
                Encoding = "UTF-8",
                ResponsePath = "ResponseMessage.xml",
                Timeout = 10
            };

            testCase.ExecutionSteps.Add(outMsgStep);

            BizUnit.Core.TestRunner testRunner = new BizUnit.Core.TestRunner(testCase);

            testRunner.Run();
        }
    }
}
