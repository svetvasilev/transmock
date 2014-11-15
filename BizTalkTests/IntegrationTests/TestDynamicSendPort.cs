using System;

using BizUnit;
using TransMock.Integration.BizUnit;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizTalkTest.IntegrationTests
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
            var testCase = new BizUnit.Xaml.TestCase();

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

            BizUnit.BizUnit testRunner = new BizUnit.BizUnit(testCase);

            testRunner.RunTest();
        }

        [TestMethod]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        public void TestHappyPath_OneWay_HelperClass()
        {
            var testCase = new BizUnit.Xaml.TestCase();

            var inMsgStep = new MockSendStep()
            {
                Url = BizTalkTests.Test.BizTalkTestsMockAddresses.BTS_OneWayReceive_FILE,
                RequestPath = "StartMessage.xml",
                Encoding = "UTF-8"
            };

            testCase.ExecutionSteps.Add(inMsgStep);

            var outMsgStep = new MockReceiveStep()
            {
                Url = BizTalkTests.Test.BizTalkTestsMockAddresses.DynamicPortOut,
                Encoding = "UTF-8",
                Timeout = 10
            };

            testCase.ExecutionSteps.Add(outMsgStep);

            BizUnit.BizUnit testRunner = new BizUnit.BizUnit(testCase);

            testRunner.RunTest();
        }

        [TestMethod]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        [DeploymentItem(@"TestData\ResponseMessage.xml")]
        public void TestHappyPath_TwoWay_HelperClass()
        {
            var testCase = new BizUnit.Xaml.TestCase();

            var inMsgStep = new MockSendStep()
            {
                Url = BizTalkTests.Test.BizTalkTestsMockAddresses.BTS_OneWayReceive2_FILE,
                RequestPath = "StartMessage.xml",
                Encoding = "UTF-8"
            };

            testCase.ExecutionSteps.Add(inMsgStep);

            var outMsgStep = new MockRequestResponseStep()
            {
                Url = BizTalkTests.Test.BizTalkTestsMockAddresses.DynamicPortOut2Way,                
                Encoding = "UTF-8",
                ResponsePath = "ResponseMessage.xml",
                Timeout = 10
            };

            testCase.ExecutionSteps.Add(outMsgStep);

            BizUnit.BizUnit testRunner = new BizUnit.BizUnit(testCase);

            testRunner.RunTest();
        }
    }
}
