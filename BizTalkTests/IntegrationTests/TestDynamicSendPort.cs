using System;

using BizUnit;
using TransMock.Integration.BizUnit;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizTalkTest.IntegrationTests
{
    [TestClass]
    public class TestDynamicSendPort
    {
        TransMockExecutionBeacon
                testbeacon;

        [TestInitialize]
        public void TestSetup()
        {
            testbeacon = new TransMockExecutionBeacon();

            testbeacon.StartBecon();

        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (testbeacon != null)
                testbeacon.StopBeacon();
        }

        [TestMethod]
        [DeploymentItem(@"TestData\StartMessage.xml")]
        public void TestHappyPath()
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
    }
}
